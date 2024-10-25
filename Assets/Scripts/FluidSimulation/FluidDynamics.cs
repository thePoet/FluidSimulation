using System;
using FluidSimulation.Internal;
using UnityEngine;
using FluidDemo; // TODO: pois

namespace FluidSimulation
{
    
    /// <summary>
    /// Interface for particle based fluid simulation run on GPU. Simulation also provides proximity alerts if particles of given substances
    /// come close to each other.
    /// </summary>
    public class FluidDynamics 
    {
        
        private readonly ShaderManager _shaderManager;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        
        //TODO: parameters could be combined into a single type
        public FluidDynamics(SimulationSettings simulationSettings, Substance[] substances, ProximityAlertRequest[] alerts = null)
        {
            var settings = ConvertSimulationSettings(simulationSettings);
    
            _shaderManager = new ShaderManager("FluidDynamicsComputeShader", settings, ToInternalFluids(substances), alerts);
        }

        public void Dispose()
        {
            _shaderManager.Dispose();
        }

        public void Step(float deltaTime, FluidSimParticle[] particles)
        {
            _shaderManager.Step(deltaTime, particles);
        }
        

        public Span<ProximityAlert> ProximityAlerts => _shaderManager.GetProximityAlerts();
        

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

        private SimulationSettingsInternal ConvertSimulationSettings(SimulationSettings simulationSettings)
        {
            var ssi = new SimulationSettingsInternal();
            var ss = simulationSettings;
             
            ssi.InteractionRadius = 3.5f * ss.Scale;
            ssi.AreaBounds = ss.AreaBounds;
            ssi.Gravity = ss.Gravity;
            ssi.Drag = 0.001f;
            ssi.MaxNumParticles = simulationSettings.MaxNumParticles;
            ssi.IsViscosityEnabled = simulationSettings.IsViscosityEnabled;
            ssi.NumSubSteps = 3;
            ssi.MaxNumParticlesInPartitioningCell = 40;
            ssi.MaxNumNeighbours = 50;
            ssi.SolidRadius = ss.SolidRadius;
            return ssi;
        }
        
        FluidInternal[] ToInternalFluids(Substance[] fluids)
        {
            var internalFluids = new FluidInternal[fluids.Length];
            for (int i = 0; i < fluids.Length; i++)
            {
                internalFluids[i] = ConvertFluid(fluids[i]);
            }
            return internalFluids;
        }
        
        private FluidInternal ConvertFluid(Substance substance)
        {
            var f = new FluidInternal();

            f.Mass = substance.Density;

            if (substance is Liquid)
            {
                f.State = 0;
                f.Stiffness = 2000f;
                f.NearStiffness = 4000f;
                f.RestDensity = 5f;
                f.DensityPullFactor = 0.5f;

                f.ViscositySigma = 0.2f * (substance as Liquid).Viscosity;
                f.ViscosityBeta = 0.2f * (substance as Liquid).Viscosity;

                f.GravityScale = 1f;
            }

            if (substance is Gas)
            {
                f.State = 1;
                f.Stiffness = 200f;
                f.NearStiffness = 400f;
                f.RestDensity = 5f;
                f.DensityPullFactor = 0.5f;
                
                f.ViscositySigma = 0.2f * (substance as Gas).Viscosity;
                f.ViscosityBeta = 0.2f * (substance as Gas).Viscosity;

                f.GravityScale = -0.05f;
            }

            if (substance is Solid)
            {
                f.State = 2;
                f.Stiffness = 1f;
                f.NearStiffness = 1f;
                f.RestDensity = 5f;
                f.DensityPullFactor = 0f;

                f.GravityScale = 0f;
            }
        
                
            return f;
        }
        
        #endregion

   
    }
}