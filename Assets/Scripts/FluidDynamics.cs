using System;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;


namespace FluidSimulation
{
    
    public class FluidDynamics 
    {
        private FluidsComputeShader _computeShader;
        private ParticleData _particleData;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public FluidDynamics(SimulationSettings simulationSettings, Fluid[] fluids)
        {
            _computeShader = new FluidsComputeShader("FluidDynamicsComputeShader", simulationSettings, fluids);
            _particleData = new ParticleData(simulationSettings);

        }
   
        public void EndSimulation()
        {
            _computeShader.Dispose();
        }
        public void Step(float timeStep)
        {
            _computeShader.Step(timeStep, _particleData);
        }

        public Span<FluidParticle> Particles => _particleData.All();
        
        public int AddParticle(FluidParticle particle)
        {
            return _particleData.Add(particle);
        }
        
        public void RemoveParticle(int particleIndex)
        {
            throw new NotImplementedException();
        }
        
        public Span<int> ParticlesAt(Vector2 position, float radius)
        {
            throw new NotImplementedException();
        }
        
        public void Clear()
        {
            _particleData.Clear();
        }    

        #endregion


    
    }
}
