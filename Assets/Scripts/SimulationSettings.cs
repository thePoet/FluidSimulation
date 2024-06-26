using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
    
namespace FluidSimulation
{
    public struct SimulationSettings
    {
        public float InteractionRadius;
        public float Gravity;
        public int MaxNumParticles;
        public Rect AreaBounds;
        public int MaxNumParticlesInPartitioningCell;
        public bool IsViscosityEnabled;
        public int NumDensityDisplacementRounds;

        // TODO: MOVE:
        public Grid2D PartitioningGrid => new Grid2D(AreaBounds, squareSize: InteractionRadius);

        public int MaxNumNeighbours;
    }

}