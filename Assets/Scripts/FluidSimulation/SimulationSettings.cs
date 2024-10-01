using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
    
namespace FluidSimulation
{
    public struct SimulationSettings
    {
        public float InteractionRadius;
        public float Gravity;
        public float Drag;
        public int MaxNumParticles;
        public Rect AreaBounds;
        public int MaxNumParticlesInPartitioningCell;
        public bool IsViscosityEnabled;
        public int NumSubSteps;
        public int MaxNumNeighbours;
    }

}