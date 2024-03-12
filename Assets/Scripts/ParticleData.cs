using System;
using System.Linq;
using UnityEngine;

namespace FluidSimulation
{
    public class ParticleData : IParticleData
    {
        private readonly int _maxNumParticles;
        private readonly int _maxNumNeighbours;
        private readonly int _maxNumParticlesInSpatialCell;
        private int _numParticles = 0;
        
        private readonly float _neighbourRadius;
        private readonly FluidParticle[] _particles;
        
        
        private int _nextId = 0;
        private readonly NeighbourSearch _neighbourSearch;
        private readonly Rect _bounds;
 
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleData(int maxNumParticles, int maxNumNeighbours, float neighbourRadius, Rect bounds)
        {
            _maxNumParticles = maxNumParticles;
            _maxNumNeighbours = maxNumNeighbours;
            _neighbourRadius = neighbourRadius;
            _maxNumParticlesInSpatialCell = maxNumNeighbours * 2;
            _bounds = bounds;
            _particles = new FluidParticle[maxNumParticles];
            _neighbourSearch = new NeighbourSearch(neighbourRadius, maxNumParticles, maxNumNeighbours);
        }

        

        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.PreviousPosition = particle.Position;
            particle.Id = _nextId;
            _nextId++;
            _numParticles++;
            int index = _numParticles - 1;
            _particles[index] = particle;
            
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
            return _neighbourSearch.NeighboursOf(particleIndex);
        }

        public void UpdateNeighbours()
        {
            _neighbourSearch.UpdateNeighbours(All());
        }

        public void Clear()
        {
            _numParticles = 0;
        }
        
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion
        
    }
}
