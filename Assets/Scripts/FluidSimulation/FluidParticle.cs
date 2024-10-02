using UnityEngine;

namespace FluidSimulation
{
    public struct FluidParticle
    {
        public int Id; 
        public Vector2 Position;
        public Vector2 Velocity;
        public int FluidIndex;  //This is the idx of the fluid in the array that is provided for FluidDynamics constructor.
        
        public static int Stride => 2*sizeof(int) + 4 * sizeof(float);

    }
    
    

}
