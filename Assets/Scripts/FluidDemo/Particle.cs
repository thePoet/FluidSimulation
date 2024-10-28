using UnityEngine;

namespace FluidDemo
{
    public struct Particle 
    {
        public ParticleId Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public SubstanceId SubstanceId;
        public GameObject Visuals;
    }
}
