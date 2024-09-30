using System;
using FluidSimulation.Internal;
using UnityEngine;


namespace FluidSimulation
{
    public class FluidParticles
    {
        public int MaxNumParticles { get; init; }
        public int NumParticles { get; private set; }
        
        private FluidParticle[] _particles;
        private int _nextId = 0;
        private SpatialPartitioningGrid<int> _spatialPartitioning;

        public FluidParticles(int maxNumParticles, SpatialPartitioningGrid<int> partitioning)
        {
            MaxNumParticles = maxNumParticles;
            NumParticles = 0;
            _particles = new FluidParticle[maxNumParticles];
            _spatialPartitioning = partitioning;
        }
        
        // TODO: Jokin muu tapa?
        public Span<FluidParticle> Particles => _particles.AsSpan().Slice(0, NumParticles);
        
        
        public FluidParticle Get(int index) => _particles[index];
        
        public int Add(FluidParticle particle)
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
        
        public int[] InsideCircle(Vector2 position, float radius) => _spatialPartitioning.CircleContents(position, radius);

        public void WriteToComputeBuffer(ComputeBuffer buffer)
        {
            buffer.SetData(_particles);
        }

        public void ReadFromComputeBuffer(ComputeBuffer buffer)
        {
            buffer.GetData(_particles);  
            UpdateSpatialPartitioningGrid();
        }
        
        // TODO: Read from compute buffer instead.
        private void UpdateSpatialPartitioningGrid()
        {
            _spatialPartitioning.Clear();
            for (int i = 0; i < NumParticles; i++)
            {
                _spatialPartitioning.Add(i);
            }
        }


    }
}