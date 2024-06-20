using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle
    {

        public int Id;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public int typeNumber;
       // public Vector4 color;

        
        public static int Stride => 2*sizeof(int) + 6 * sizeof(float);
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
