using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FluidSimulation
{
    public class ParticleData 
    {
        private readonly int _maxNumParticles;
        private readonly int _maxNumNeighbours;
        private int _numParticles = 0;
        
        private readonly float _neighbourRadius;
        private  FluidParticle[] _particles;
        private int[][] _neighbours;
        private int[] _neighbourCount;
        private (int, int)[] _particlePairs;
        private int _nextId = 0;
        public SpatialPartitioning _partitioning;

        public ParticleData(int maxNumParticles, int maxNumNeighbours, float neighbourRadius)
        {
            _maxNumParticles = maxNumParticles;
            _maxNumNeighbours = maxNumNeighbours;
            _neighbourRadius = neighbourRadius;

            
            _particles = new FluidParticle[maxNumParticles];
            
            _neighbours = new int[maxNumParticles][];
            _neighbourCount = new int[maxNumParticles];
            for (int i = 0; i < maxNumParticles; i++)
            {
                _neighbours[i] = new int[maxNumNeighbours];
                _neighbourCount[i] = 0;
            }
       
            int maxNumParticlePairs = (int)(maxNumParticles * maxNumNeighbours * 0.5f) + 1;
            _particlePairs = new (int, int)[maxNumParticlePairs];
            
            
            _partitioning = new SpatialPartitioning(neighbourRadius);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.Id = _nextId;
            _nextId++;
            
            _numParticles++;
            int index = _numParticles - 1;
            
            _particles[index] = particle;
            _neighbourCount[index] = 0;
            
            _partitioning.AddEntity(particle.Id, particle.Position);
            return particle.Id;
        }

        public void Remove(int particleIndex)
        {
            throw new NotImplementedException();
        }

    

    
        public Span<FluidParticle> All()
        {
            var span = (Span<FluidParticle>)_particles;
            return span.Slice(0, _numParticles);
        }

        public Span<int> NeighbourIndices(int particleIndex)
        {
            var span =  (Span<int>)_neighbours[particleIndex];
            return span.Slice(0, _neighbourCount[particleIndex]);
        }

        public Span<(int, int)> NeighbourParticlePairs()
        {
            int p = 0;
            
            Span<(int, int)> pairs = _particlePairs;
            
            for (int indexA = 0; indexA < _numParticles; indexA++)
            {
                for (int i = 0; i < _neighbourCount[indexA]; i++)
                {
                    int indexB = _neighbours[indexA][i];
                    if (_particles[indexA].Id > _particles[indexB].Id)
                    {
                        pairs[p] = (indexA, indexB);
                        p++;
                    }
                }
            }

            return pairs.Slice(0, p);
        }
        
        public void UpdateNeighbours()
        {
            for (int i = 0; i < _numParticles; i++)
                _partitioning.UpdateEntity(i, _particles[i].Position);
            for (int i = 0; i < _numParticles; i++)
            {
                var neighbours = _partitioning.GetEntitiesInNeighbourhoodOf(_particles[i].Position).ToArray();
                neighbours.CopyTo(_neighbours[i],0);
                _neighbourCount[i] = neighbours.Length;
            }
            
        }

        public string NeighbourhoodWatch()
        {
            string result = "";

            for (int i = 0; i < _numParticles; i++)
                result += _neighbourCount[i] + " ";
            return result;


        }
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        #endregion
     
    }
}
