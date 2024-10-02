using FluidSimulation.Internal;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace FluidSimulation
{
    
    public class FluidDynamics 
    {
        
        public readonly FluidParticles Particles;   
        private readonly ShaderManager _shaderManager;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        public FluidDynamics(SimulationSettings settings, Fluid[] fluids)
        {
            var partitioningGrid = new SpatialPartitioningGrid<int>(
                new Grid2D(settings.AreaBounds, squareSize: settings.InteractionRadius),
                settings.MaxNumParticlesInPartitioningCell,
                i => Particles.Get(i).Position);

            _shaderManager = new ShaderManager("FluidDynamicsComputeShader", settings, ToInternalFluids(fluids), partitioningGrid.NumSquares);
            
            Particles = new FluidParticles(settings.MaxNumParticles, partitioningGrid);
        }

        public void Dispose()
        {
            _shaderManager.Dispose();
        }

        public void Step(float deltaTime)
        {
            _shaderManager.Step(deltaTime, Particles, Particles.NumParticles);
        }

        /// <summary>
        /// The subscribed debug data for the given particle. The data is available after next Step-method call.
        /// </summary>
        /// <param name="particleId"></param>
        public void SubscribeDebugData(int particleId)
        {
            _shaderManager.SelectedParticle = particleId;
        }

        public Vector2[] DebugData()
        {
            return _shaderManager.GetSelectedParticleData();
        }
        
        #endregion
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