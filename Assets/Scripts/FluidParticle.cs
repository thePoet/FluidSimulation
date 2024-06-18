using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle
    {

        public int Id;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public Vector2 Change;
        public float Pressure;
        public float NearPressure;
        public int typeNumber;
        public Vector4 color;

        public static int Stride => sizeof(int) + 10 * sizeof(float) + sizeof(int) + sizeof(float) * 4;
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
