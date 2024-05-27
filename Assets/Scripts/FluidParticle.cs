using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle : IPositionAndId
    {
        public int Id { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public Vector2 PosChange;
        public ParticleType Type;
    }
    
    public enum ParticleType
    {
        Liquid, Solid
    }
    
}
