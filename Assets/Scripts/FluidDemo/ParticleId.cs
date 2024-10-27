
using System;

namespace FluidDemo
{
    
    public struct ParticleId : IEquatable<ParticleId>
    {
        private static int NextId = 0;
        
        public int Id { get; init; }

        public bool Equals(ParticleId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is ParticleId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
        
        public static ParticleId CreateNewId()
        {
            return new ParticleId {Id = NextId++};
        }
        
        
    }
}