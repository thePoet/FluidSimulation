using FluidSimulation.Internal;
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
            ShaderManager = new ShaderManager("FluidDynamicsComputeShader", settings, ToInternalFluids(fluids));
           
            var partitioningGrid = new SpatialPartitioningGrid<int>(
                settings.PartitioningGrid,
                settings.MaxNumParticlesInPartitioningCell,
                i => Particles.Get(i).Position);

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

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

      
      
        public void SelectParticle(int particleId)
        {
            ShaderManager.SelectedParticle = particleId;
        }

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