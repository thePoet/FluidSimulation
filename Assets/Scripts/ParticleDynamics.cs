using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation

namespace FluidSimulation
{
    
    public class ParticleDynamics : IParticleDynamics
    {
        [System.Serializable]
        public class Settings
        {
            public float InteractionRadius;
            public float Gravity;
            public float RestDensity;
            public float Stiffness;
            public float NearStiffness;
            public float ViscositySigma;
            public float ViscosityBeta;
       
            public int MaxNumParticles;
            public Rect AreaBounds;
            public int MaxNumParticlesInPartitioningCell;

            // TODO: MOVE:
            public Grid2D PartitioningGrid => new Grid2D(AreaBounds, squareSize: InteractionRadius);
            public int MaxNumNeighbours;
        }
        
  
     
        private FluidsComputeShader _computeShader;
        private Rect _bounds;
        private readonly Settings _settings;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleDynamics(Settings settings, Rect bounds)
        {
            _settings = settings;
            _bounds = bounds;

            _computeShader = new FluidsComputeShader("FluidDynamicsComputeShader", settings);
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
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        

        #endregion
      
    }
}
