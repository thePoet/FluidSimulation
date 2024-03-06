using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FluidSimulation
{
    public class Particles : IParticleStorage
    {
        private readonly int _maxNumParticles;
        private readonly float _neighbourRadius;
        private readonly List<FluidParticle> _particles;
        private readonly Dictionary<int, int[]> _neighbours;
        private int _nextId = 1;
        private SpatialPartitioning _partitioning;

        public Particles(int maxNumParticles, float neighbourRadius)
        {
            _maxNumParticles = maxNumParticles;
            _neighbourRadius = neighbourRadius;
            _particles = new List<FluidParticle>();
            _neighbours = new Dictionary<int, int[]>();
            _partitioning = new SpatialPartitioning(neighbourRadius);
        }

        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.Id = _nextId;
            _nextId++;
            _particles.Add(particle);
            _neighbours.Add(particle.Id, new int[]{});
            _partitioning.AddEntity(particle.Id, particle.Position);
            return particle.Id;
        }

        

        public void Remove(int particleIndex)
        {/*
            var particle = _particles[particleIndex];
            _partitioning.RemoveEntity(particle);
            _neighbours.Remove(particleIndex);
            _particles.RemoveAt(particleIndex);*/
        }

    

    
        public Span<FluidParticle> All()
        {
            return _particles.ToArray();
        }

        public Span<int> NeighboursOf(int particleId)
        {
            return _neighbours[particleId];
        }

        public Span<(int, int)> NeighbourParticlePairs()
        {
            List<(int,int)> result = new List<(int, int)>();
            
            for (int i = 0; i < _particles.Count; i++)
            {
                for (int n = 0; n < _neighbours[_particles[i].Id].Length; n++)
                {
                    int j = _neighbours[i][n];
                    if (_particles[i].Id > _particles[j].Id)
                    {
                        result.Add((i,j));
                    }
                }
            }
            return result.ToArray();
        }
        
        public void UpdateNeighbours()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                _neighbours[i] = _partitioning.GetEntitiesInNeighbourhoodOf(_particles[i].Position).ToArray();
            }
        }
        
        public int NumberOfParticles => _particles.Count;
     
    }
}
