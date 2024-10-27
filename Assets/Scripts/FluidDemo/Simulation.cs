using System;
using UnityEngine;
using FluidSimulation;
using RikusGameDevToolbox.GeneralUse;
using TMPro;

namespace FluidDemo
{
    public class Simulation : MonoBehaviour
    {
        public TextMeshPro text;
        
        private FluidDynamics _fluidDynamics;
       // private Particles _particles;
        private ParticleCollection _particleCollection;
        private SpatialPartitioningGrid<ParticleId> _partitioningGrid;
        private ParticleVisuals _particleVisuals;

        private FluidSimParticle[] _fspBuffer; 

        private MovingAverage _avgUpdateTime;
        
        private SimulationSettings Settings => new()
        {
            Scale = 6f,
            Gravity = 1200f,
            MaxNumParticles = 30000,
            IsViscosityEnabled = true,
            AreaBounds = new Rect(Vector2.zero, new Vector2(1200f, 600f)),
            SolidRadius = 15f
        };
        
        private const int MaxNumParticlesInPartitioningSquare = 40;
        
        public ParticleId[] ParticlesInsideRectangle(Rect rect) => _partitioningGrid.RectangleContents(rect);
        public ParticleId[] ParticlesInsideCircle(Vector2 position, float radius) => _partitioningGrid.CircleContents(position, radius);
        public Vector2[] ParticleDebugData() => _fluidDynamics.DebugData();
        public void SelectDebugParticle(int particleIdx) => _fluidDynamics.SubscribeDebugData(particleIdx);
     
        #region ------------------------------------------ UNITY METHODS ----------------------------------------------
       
        private void Awake()
        {
            _particleVisuals = FindObjectOfType<ParticleVisuals>();
            if (_particleVisuals == null) Debug.LogError("No visualization found in the scene.");

            var alerts = CreateProximityAlertSubscriptions();

            _fluidDynamics = new FluidDynamics(Settings, Fluids.List, alerts);
            _fspBuffer = new FluidSimParticle[Settings.MaxNumParticles];
            
            for (int i = 0; i < Settings.MaxNumParticles; i++)
            {
                _fspBuffer[i] = new FluidSimParticle
                {
                    Position = Vector2.zero,
                    Velocity = Vector2.zero,
                    SubstanceIndex = -1,
                    Id = -1,
                    Active = false
                };
            }
            
            _partitioningGrid = CreateSpatialPartitioningGrid();
          //  _particles = new Particles(Settings.MaxNumParticles, _partitioningGrid);
            _particleCollection = new ParticleCollection(Settings.MaxNumParticles);
            
            _avgUpdateTime = new MovingAverage(300);
        }


        void Update()
        {
            float t = Time.realtimeSinceStartup;
           
            SimulateFluids(0.015f, _fluidDynamics, _particleCollection);
            DoSpatialPartitioning(_partitioningGrid, _particleCollection);
            UpdateParticleVisualization();
            
            HandleProximityAlerts(_fluidDynamics.ProximityAlerts);
            
            t = Time.realtimeSinceStartup - t;
            _avgUpdateTime.Add(t);
            float avgUpdate = _avgUpdateTime.Average()*1000f;

            text.text = "Particles: " + _particleCollection.Count + " - " + avgUpdate.ToString("0.0") +
                        " ms.";
        }

    


        private void OnDisable()
        {
            _fluidDynamics.Dispose();
        }


        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        

        public Particle SpawnParticle(Vector2 position, Vector2 velocity, FluidId fluidId)
        {
            var particle = new Particle();
            particle.Id = ParticleId.CreateNewId();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.FluidId = fluidId;
            
//            _particles.Add(particle);
            _particleCollection.Add(particle);
            _particleVisuals.Add(particle.Id, fluidId, position);

            return particle;
        }

      
        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var id in ParticlesInsideCircle(position, radius))
            {
                var p = _particleCollection.Get(id);
                p.Velocity = velocity;
                _particleCollection.Update(p);
            }
        }

        public void Clear()
        {
            _particleCollection.Clear();
            _particleVisuals.Clear();
        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void SimulateFluids(float deltaTime, FluidDynamics fluidDynamics, ParticleCollection particles)
        {
            var span = particles.TempGetSpan();
            for (int i=0; i<Settings.MaxNumParticles; i++)
            {
                if (i < span.Length)
                {
                    _fspBuffer[i].Active = true;
                    _fspBuffer[i].Position = span[i].Position;
                    _fspBuffer[i].Velocity = span[i].Velocity;
                    _fspBuffer[i].SetFluid(span[i].FluidId);
                }
                else
                {
                    _fspBuffer[i].Active = false;
                }
            }
           
            fluidDynamics.Step(deltaTime, _fspBuffer);


            for (int i = 0; i < span.Length; i++)
            {
                span[i].Position = _fspBuffer[i].Position;
                span[i].Velocity = _fspBuffer[i].Velocity;
            }

            particles.TempSaveSpan(span);
            
            //_particles.SimulateFluids(timestep: 0.015f, _fluidDynamics);
        }
        
        
        private void HandleProximityAlerts(Span<ProximityAlert> proximityAlerts)
        {/*
            foreach (var alert in proximityAlerts)
            {
                int i1 = alert.IndexParticleA;
                int i2 = alert.IndexParticleB;

                ChangeParticleSubstance(i1, FluidId.Smoke);
                ChangeParticleSubstance(i2, FluidId.Smoke);
            }
               */ 
        }

        private ProximityAlertRequest[] CreateProximityAlertSubscriptions()
        {
            var pas = new ProximityAlertRequest
            {
                IndexFluidA = Fluids.IndexOf(FluidId.GreenLiquid),
                IndexFluidB = Fluids.IndexOf(FluidId.RedLiquid),
                Range = 10f
            };

            return new[]{pas};
        }

        
        private void ChangeParticleSubstance(ParticleId particleId, FluidId fluidId)
        {
            var p = _particleCollection.Get(particleId);
            p.FluidId=FluidId.Smoke;
            _particleCollection.Update(p);
            
            _particleVisuals.Delete(particleId);
            _particleVisuals.Add(particleId, fluidId, _particleCollection.Get(particleId).Position);
        }



    

        private void UpdateParticleVisualization()
        {
            foreach (var particle in _particleCollection.TempGetSpan())
            {
                _particleVisuals.UpdateParticle(particle.Id, particle.Position);
            }
        }


    
  
        
        private SpatialPartitioningGrid<ParticleId> CreateSpatialPartitioningGrid()
        {
            float squareSize = 3.5f * Settings.Scale;
            return new SpatialPartitioningGrid<ParticleId>(Settings.AreaBounds, squareSize, MaxNumParticlesInPartitioningSquare);
        }

        private void DoSpatialPartitioning(SpatialPartitioningGrid<ParticleId> grid, ParticleCollection particles)
        {
            grid.Clear();
           /* for (int i = 0; i < particles.NumParticles; i++)
            {
                grid.Add(i, particles[i].Position);
            }*/
           
            foreach (var particle in particles.TempGetSpan())
            {
                grid.Add(particle.Id, particle.Position);
            }
        }
        
        #endregion


    }
}


