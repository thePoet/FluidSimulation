using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation

namespace FluidSimulation
{
    
    public class FluidDynamics 
    {
      
        public struct SimulationSettings
        {
            public float InteractionRadius;
            public float Gravity;
            public int MaxNumParticles;
            public Rect AreaBounds;
            public int MaxNumParticlesInPartitioningCell;

            // TODO: MOVE:
            public Grid2D PartitioningGrid => new Grid2D(AreaBounds, squareSize: InteractionRadius);
            public int MaxNumNeighbours;
        }

      
     
        private FluidsComputeShader _computeShader;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public FluidDynamics(SimulationSettings simulationSettings, Fluid[] fluids)
        {
            _computeShader = new FluidsComputeShader("FluidDynamicsComputeShader", simulationSettings, fluids);
        }
   
        public void Dispose()
        {
            _computeShader.Dispose();
        }
        public void Step(ParticleData particleData, float timeStep)
        {
            _computeShader.Step(timeStep, particleData);
        }

        #endregion
    
      
    }
}
