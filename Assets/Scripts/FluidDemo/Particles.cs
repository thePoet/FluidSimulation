using System;
using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public class Particles
    {
        public int MaxNumParticles { get; init; }
        public int NumParticles { get; private set; }

        private Particle[] _particles;
        private int _nextId = 0;
        private SpatialPartitioningGrid<int> _spatialPartitioning;

        public Particles(int maxNumParticles, SpatialPartitioningGrid<int> partitioning)
        {
            MaxNumParticles = maxNumParticles;
            NumParticles = 0;
            _particles = new Particle[maxNumParticles];
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new Particle
                {
                    Position = Vector2.zero,
                    Velocity = Vector2.zero,
                    FluidIndex = -1,
                    Id = -1,
                    Active = false
                };

              
            }
            _spatialPartitioning = partitioning;
        }
        
        
        
        public Particle this[int index] 
        {
            get => _particles[index];
            set => _particles[index] = value;
        }
       
        public Span<Particle> Span => _particles.AsSpan().Slice(0, NumParticles);

        public Particle[] FluidDynamicsParticles => _particles;

        public int Add(Particle particle)
        {
            //TODO: kunnolla
            particle.Id = _nextId;
            _nextId++;
            NumParticles++;
            int index = NumParticles - 1;
            _particles[index] = particle;
            
            _spatialPartitioning.Add(index);
            
            return particle.Id;
        }

        public void Remove(int id)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            NumParticles = 0;
        }
        
        public int[] InsideRectangle(Rect rect) => _spatialPartitioning.RectangleContents(rect);
        
        public int[] InsideCircle(Vector2 position, float radius) => _spatialPartitioning.CircleContents(position, radius);

      
        public void UpdateSpatialPartitioningGrid()
        {
            _spatialPartitioning.Clear();
            for (int i = 0; i < NumParticles; i++)
            {
                if (_particles[i].Active) _spatialPartitioning.Add(i);
            }
        }


    }
}