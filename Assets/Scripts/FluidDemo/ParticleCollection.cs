using System;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace FluidDemo
{
    public class ParticleCollection
    {
        private Dictionary<ParticleId, Particle> _particles;
      
        private Particle[] _array;

        public int Count => _particles.Count;
        
        public ParticleCollection(int maxNumParticles)
        {
            _particles = new Dictionary<ParticleId, Particle>();
            _array = new Particle[maxNumParticles];
        }


        public Particle Get(ParticleId id)
        {
            return _particles.GetValueOrDefault(id);
        }
        
        public void Add(Particle particle)
        {
            _particles.Add(particle.Id, particle);
        }
        
        public void Remove(ParticleId id)
        {
            _particles.Remove(id);
        }

        public void Update(Particle particle)
        {
            _particles[particle.Id] = particle;
        }


        public void Clear()
        {
            _particles.Clear();
        }

        public Span<Particle> TempGetSpan()
        {
            int i = 0;
            foreach (var particle in _particles.Values)
            {
                _array[i] = particle;
                i++;
            }
            return _array.AsSpan().Slice(0, i);
        }

        public void TempSaveSpan(Span<Particle> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                Update(span[i]);
            }
            
        }

      
        
    }
}