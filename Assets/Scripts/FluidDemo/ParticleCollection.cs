using System;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace FluidDemo
{
    /// <summary>
    /// This class is responsible for tracking all the particles in the world.
    /// </summary>
    public class ParticleCollection
    {
        // TODO: these could be replaced with arrays for performance:
        private Dictionary<ParticleId, Particle> _particles; 
        private Dictionary<int, Particle> _fluidSimParticleIdxToParticle;
        
        private SpatialPartitioningGrid<Particle> _spatialPartitioning;
        
        public int MaxNumParticles { get; private set; }
        public int NumParticles => _particles.Count;
        
        
        public ParticleCollection(int maxNumParticles, Grid2D partitioningGrid, int maxNumParticlesInSquare)
        {
            MaxNumParticles = maxNumParticles;
            _spatialPartitioning = new SpatialPartitioningGrid<Particle>(partitioningGrid, maxNumParticlesInSquare);
            _particles = new Dictionary<ParticleId, Particle>();
            _fluidSimParticleIdxToParticle = new Dictionary<int, Particle>();
        }
        
        public void Add(Particle particle)
        {
            _particles.Add(particle.Id, particle);
            _fluidSimParticleIdxToParticle.Add(particle.FluidSimParticleIdx, particle);
            _spatialPartitioning.Add(particle, particle.Position);
        }
        
        public void Remove(Particle particle)
        {
            _particles.Remove(particle.Id);
            _fluidSimParticleIdxToParticle.Remove(particle.FluidSimParticleIdx);
        }

        public Particle Get(ParticleId id)
        {
            return _particles.GetValueOrDefault(id);
        }

        public Particle WithFluidSimParticleIndex(int idx)
        {
            return _fluidSimParticleIdxToParticle.GetValueOrDefault(idx);
        }

        public IEnumerable<Particle> AllParticles()
        {
            foreach (var particle in _particles.Values)
            {
                yield return particle;
            }
        }

        public IEnumerable<Particle> FluidParticles()
        {
            throw new NotImplementedException();
        }
        
        public IEnumerable<Particle> SolidParticles()
        {
            throw new NotImplementedException();
        }
        
        public void UpdateSpatialPartitioning()
        {
            _spatialPartitioning.Clear();
            foreach (var particle in _particles.Values)
            {
                _spatialPartitioning.Add(particle, particle.Position);
            }
        }

        /// <summary>
        /// Note: Spatial partitioning needs to be manuallu updateted with UpdateSpatialPartitioning() after
        /// moving or deleting particles for this to work correctly.
        /// </summary>
        public Particle[] InsideRectangle(Rect rect) => _spatialPartitioning.RectangleContents(rect);
        
        /// <summary>
        /// Note: Spatial partitioning needs to be manuallu updateted with UpdateSpatialPartitioning() after
        /// moving or deleting particles for this to work correctly.
        /// </summary>
        public Particle[] InsideCircle(Vector2 position, float radius) => _spatialPartitioning.CircleContents(position, radius);

        public int GetUnusedParticleIdNumber()
        {
            throw new NotImplementedException();
        }
        
    }
}