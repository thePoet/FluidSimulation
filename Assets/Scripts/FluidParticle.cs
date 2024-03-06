using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle : ISpatiallyPartible
    {
        public int Id { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public ParticleType Type;
    }
    
}
