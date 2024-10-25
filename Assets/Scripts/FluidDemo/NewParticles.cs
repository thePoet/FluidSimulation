using System;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace FluidDemo
{
    public class NewParticles
    {
        private Dictionary<ParticleId, Particle> _particles;
      //  private Dictionary<int, Particle> _fspIdxToParticle;
        private SpatialPartitioningGrid<Particle> _spatialPartitioning;
        
        public NewParticles(int maxNumParticles, Grid2D partitioningGrid)
        {/*
            _spatialPartitioning = new SpatialPartitioningGrid<Particle>(
                partitioningGrid, 40,
                p => p.Position);
            
            */
            _particles = new Dictionary<ParticleId, Particle>();
        }

        public Particle Get(ParticleId id)
        {
            return _particles.GetValueOrDefault(id);
        }

        public Particle WithFluidSimParticleIndex(int idx)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Particle> AllParticles()
        {
            foreach (var (id, particle) in _particles)
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

        public Particle[] InsideRectangle(Rect rect) => _spatialPartitioning.RectangleContents(rect);
        
        public Particle[] InsideCircle(Vector2 position, float radius) => _spatialPartitioning.CircleContents(position, radius);

        
    }
}