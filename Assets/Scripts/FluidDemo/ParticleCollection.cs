using System;
using System.Collections.Generic;

namespace FluidDemo
{
    public class ParticleCollection
    {
        private Dictionary<ParticleId, int> _IdToIndex;
        private Dictionary<int, ParticleId> _IndexToId;
        private Particle[] _array;

        public int Count => _IdToIndex.Count;
        
        public ParticleCollection(int maxNumParticles)
        {
            _IdToIndex = new Dictionary<ParticleId, int>();
            _IndexToId = new Dictionary<int, ParticleId>();
            _array = new Particle[maxNumParticles];
        }


        public Particle Get(ParticleId id)
        {
            return _array[_IdToIndex[id]];
        }
        
        public void Add(Particle particle)
        {
            int index = _IdToIndex.Count;
            _array[index] = particle;
            _IdToIndex.Add(particle.Id, index);
            _IndexToId.Add(index, particle.Id);
        }

        public void Remove(ParticleId id)
        {
            int index = _IdToIndex[id];
            int lastIndex = _IdToIndex.Count - 1;

            _IdToIndex.Remove(id);
            _IndexToId.Remove(index);
            
            if (index == lastIndex) return;
            
            // Move last element in array to the empty spot so that the array is contiguous:
            _array[index] = _array[lastIndex];

            //..and adjust the dictionaries accordingly:
            ParticleId lastElementId = _IndexToId[lastIndex];
            _IdToIndex[lastElementId] = index;
            _IndexToId[index] = lastElementId;
            _IndexToId.Remove(lastIndex);
        }

        public void Update(Particle particle)
        {
            _array[_IdToIndex[particle.Id]] = particle;
        }

        public void Clear() => _IdToIndex.Clear();
        

        public Span<Particle> AsSpan()
        {
            return _array.AsSpan().Slice(0, _IdToIndex.Count);
        }
        
    }
}