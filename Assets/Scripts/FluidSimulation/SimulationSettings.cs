using UnityEngine;
    
namespace FluidSimulation
{
    public struct SimulationSettings
    {
        public Rect AreaBounds;
        /// <summary>The scale is the spacing of particles i.e. the typical distance between them.</summary>
        public float Scale;
        public float Gravity;
        public int MaxNumParticles;
        public bool IsViscosityEnabled;
        public float SolidRadius;
    }

}