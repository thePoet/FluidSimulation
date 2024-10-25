
using System;

namespace FluidDemo
{
    public struct ParticleId : IEquatable<ParticleId>
    {
        public int Id { get; private init; }

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

        public static ParticleId New(ParticleCollection particleCollection)
        {
            return new ParticleId{Id = particleCollection.GetUnusedParticleIdNumber()};
        }
    }

}