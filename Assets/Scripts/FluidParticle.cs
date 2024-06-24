using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public int FluidIndex;
        
        public static int Stride => 2*sizeof(int) + 4 * sizeof(float);

    }
    

}
