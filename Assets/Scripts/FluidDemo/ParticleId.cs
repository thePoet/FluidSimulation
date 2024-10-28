using System;

namespace FluidDemo
{
    public readonly struct ParticleId : IEquatable<ParticleId>
    {
        public static ParticleId CreateUnique() => new ParticleId {Id = Guid.NewGuid()};

        public Guid Id { get; init; }

        public bool Equals(ParticleId other) => Id.Equals(other.Id);
        public override bool Equals(object obj) => obj is ParticleId other && Equals(other);
        public override int GetHashCode()  => Id.GetHashCode();
    }
}
    
