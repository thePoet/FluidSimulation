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

        /*
        public void WriteToComputeBuffer(ComputeBuffer buffer)
        {
            buffer.SetData(_particles);
        }

        public void ReadFromComputeBuffer(ComputeBuffer buffer)
        {
            buffer.GetData(_particles);  
            UpdateSpatialPartitioningGrid();
        }
        */

        public void UpdateSpatialPartitioningGrid()
        {
            _spatialPartitioning.Clear();
            for (int i = 0; i < NumParticles; i++)
            {
                _spatialPartitioning.Add(i);
            }
        }


    }
}