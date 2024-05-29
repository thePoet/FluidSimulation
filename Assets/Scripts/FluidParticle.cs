using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle
    {

        public int Id;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public Vector2 PosChange;
        public int typeNumber;

        public static int Stride => sizeof(int) + 8 * sizeof(float) + sizeof(int);
        public ParticleType Type
        {
            get => (ParticleType)typeNumber;
            set => typeNumber = (int)value;
        }
        

    }
    
    
    public enum ParticleType
    {
        Liquid, Solid
    }
    
}
