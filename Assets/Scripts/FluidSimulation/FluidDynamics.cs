using FluidSimulation.Internal;
using UnityEngine;

namespace FluidSimulation
{
    
    public class FluidDynamics 
    {
        public readonly FluidParticles Particles;   

        private ShaderManager ShaderManager;

        private int _selectedParticle = -1;
        
        
            
            
        public FluidDynamics(SimulationSettings settings, FluidInternal[] fluids)
        {
           ShaderManager = new ShaderManager("FluidDynamicsComputeShader", settings, fluids);


            var partitioningGrid = new SpatialPartitioningGrid<int>(
                settings.PartitioningGrid,
                settings.MaxNumParticlesInPartitioningCell,
                i => Particles.Get(i).Position);

            Particles = new FluidParticles(settings.MaxNumParticles, partitioningGrid);
        }

        ~FluidDynamics()
        {
            ShaderManager.Dispose();
        }

        public void Step(float deltaTime)
        {
            ShaderManager.Step(0.015f, Particles, Particles.NumParticles);
        }
        


  
        
    

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        


        

        public int[] ParticleIdsInsideCircle(Vector2 position, float radius) => Particles.InsideCircle(position, radius);
      
        public void SelectParticle(int particleId)
        {
            ShaderManager.SelectedParticle = particleId;
        }
        
        

        
        #endregion

   
    }
}