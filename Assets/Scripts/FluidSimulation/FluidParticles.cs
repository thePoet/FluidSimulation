using System;
using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{

    public class FluidParticles
    {
        
        public int MaxNumParticles { get; init; }
        public int NumParticles { get; private set; }
        
        private FluidParticle[] _particles;
        private int _nextId = 0;

        public FluidParticles(int maxNumParticles)
        {
            MaxNumParticles = maxNumParticles;
            NumParticles = 0;
            _particles = new FluidParticle[maxNumParticles];
        }
        
        // TODO: Jokin muu tapa?
        public Span<FluidParticle> Particles => _particles.AsSpan().Slice(0, NumParticles);
        
        public int AddParticle(FluidParticle particle)
        {
            //TODO: kunnolla
            particle.Id = _nextId;
            _nextId++;
            NumParticles++;
            int index = NumParticles - 1;
            _particles[index] = particle;
            return particle.Id;
        }

        public void RemoveParticle(int id)
        {
            throw new NotImplementedException();
        }

        public void WriteToComputeBuffer(ComputeBuffer buffer)
        {
            buffer.SetData(_particles);
        }

        public void ReadFromComputeBuffer(ComputeBuffer buffer)
        {
            buffer.GetData(_particles);  
        }

    }
}