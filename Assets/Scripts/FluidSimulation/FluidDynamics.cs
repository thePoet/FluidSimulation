using FluidSimulation.Internal;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace FluidSimulation
{
    
    public class FluidDynamics 
    {
        public readonly FluidParticles Particles;   

        private ShaderManager ShaderManager;
        private int _selectedParticle = -1;
        

        
        public FluidDynamics(SimulationSettings settings, Fluid[] fluids)
        {
            var partitioningGrid = new SpatialPartitioningGrid<int>(
                new Grid2D(settings.AreaBounds, squareSize: settings.InteractionRadius),
                settings.MaxNumParticlesInPartitioningCell,
                i => Particles.Get(i).Position);

            ShaderManager = new ShaderManager("FluidDynamicsComputeShader", settings, ToInternalFluids(fluids), partitioningGrid.NumSquares);
            
            Particles = new FluidParticles(settings.MaxNumParticles, partitioningGrid);
        }

        public void Dispose()
        {
            ShaderManager.Dispose();
        }

        public void Step(float deltaTime)
        {
            ShaderManager.Step(deltaTime, Particles, Particles.NumParticles);
        }

        public void SubscribeDebugData(int particleId)
        {
            ShaderManager.SelectedParticle = particleId;
        }

        public Vector2[] DebugData()
        {
            return ShaderManager.GetSelectedParticleData();
        }
        
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

      
      
      

        FluidInternal[] ToInternalFluids(Fluid[] fluids)
        {
            var internalFluids = new FluidInternal[fluids.Length];
            for (int i = 0; i < fluids.Length; i++)
            {
                internalFluids[i] = FluidInternal.From(fluids[i]);
            }
            return internalFluids;
        }
        
    
        
        
        #endregion

   
    }
}