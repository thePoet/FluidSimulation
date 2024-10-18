using UnityEngine;

namespace FluidSimulation
{
    public struct Particle
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public int FluidIndex;  //This is the idx of the fluid in the array that is provided for FluidDynamics constructor.
        private int _active; // 1 for active, 0 for disabled


        public bool Active
        {
            get => _active == 1;
            set => _active = value ? 1 : 0;
        }


        public const int Stride = 4 * sizeof(float) + 2 * sizeof(int) + sizeof(float);

    }
    
    

}
