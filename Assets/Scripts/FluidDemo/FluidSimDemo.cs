using System;
using UnityEngine;
using FluidSimulation;
using RikusGameDevToolbox.GeneralUse;
using TMPro;

namespace FluidDemo
{
    public class FluidSimDemo : MonoBehaviour
    {
        public TextMeshPro text;
        
        private FluidDynamics _fluidDynamics;
        private Particles _particles;
        private SpatialPartitioningGrid<int> _partitioningGrid;
        private ParticleVisuals _particleVisuals;
  
        private bool _isPaused;

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
     
        public int[] InsideRectangle(Rect rect) => _partitioningGrid.RectangleContents(rect);
        
        public int[] InsideCircle(Vector2 position, float radius) => _partitioningGrid.CircleContents(position, radius);

        public Vector2[] ParticleDebugData() => _fluidDynamics.DebugData();
        
        public void SelectDebugParticle(int particleIdx) => _fluidDynamics.SubscribeDebugData(particleIdx);
     
        
        private void Awake()
        {
            _particleVisuals = FindObjectOfType<ParticleVisuals>();
            if (_particleVisuals == null) Debug.LogError("No visualization found in the scene.");

            var alerts = CreateProximityAlertSubscriptions();

            _fluidDynamics = new FluidDynamics(Settings, Fluids.List, alerts);
            _partitioningGrid = CreateSpatialPartitioningGrid();
            
            
            
            _particles = new Particles(Settings.MaxNumParticles, _partitioningGrid);
            Particle.FsParticles = _particles;
            Particle.ParticleVisuals = _particleVisuals;
            
            _avgUpdateTime = new MovingAverage(300);
        }


        void Update()
        {
            float t = Time.realtimeSinceStartup;
            
            if (!_isPaused)
            {
                _particles.SimulateFluids(timestep: 0.015f, _fluidDynamics);
                //_fluidDynamics.Step(0.015f, _particles.FluidDynamicsParticles, _particles.NumParticles);
                DoSpatialPartitioning(_partitioningGrid, _particles);
                UpdateParticleVisualization();
            }
            HandleUserInput();
            HandleProximityAlerts(_fluidDynamics.ProximityAlerts);
            
            t = Time.realtimeSinceStartup - t;
            _avgUpdateTime.Add(t);
            float avgUpdate = _avgUpdateTime.Average()*1000f;

            text.text = "Particles: " + _particles.NumParticles + " - " + avgUpdate.ToString("0.0") +
                        " ms.";
        }


        private void OnDisable()
        {
            _fluidDynamics.Dispose();
        }


    

        public int SpawnParticle(Vector2 position, Vector2 velocity, FluidId fluidId)
        {
            var particle = new FluidSimParticle();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.SetFluid(fluidId);
            particle.Active = true;
            int particleId = _particles.Add(particle);
            _particleVisuals.Add(particleId, fluidId, position);

            return particleId;
        }

        private void HandleProximityAlerts(Span<ProximityAlert> proximityAlerts)
        {
            foreach (var alert in proximityAlerts)
            {
                int i1 = alert.IndexParticleA;
                int i2 = alert.IndexParticleB;

                ChangeParticleSubstance(i1, FluidId.Smoke);
                ChangeParticleSubstance(i2, FluidId.Smoke);
            }
                
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

        
        private void ChangeParticleSubstance(int particleIdx, FluidId fluidId)
        {
            var p = _particles[particleIdx];
            p.SetFluid(FluidId.Smoke);
            _particles[particleIdx] = p;
            
            _particleVisuals.Delete(particleIdx);
            _particleVisuals.Add(particleIdx, fluidId,_particles[particleIdx].Position);
        }


        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in InsideCircle(position, radius))
            {
                var p = _particles[particleIdx];
                p.Velocity = velocity;
                _particles[particleIdx] = p;
            }
        }

        private void HandleUserInput()
        {
            if (Input.GetKeyDown(KeyCode.C)) Clear();
            if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused;
        }

        private void UpdateParticleVisualization()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var particle in _particles.Span)
            {
                _particleVisuals.UpdateParticle(particle.Id, particle.Position);
            }
            
            t= Time.realtimeSinceStartup-t;
            
//            Debug.Log("Updating particle visualization took" + t*1000f + " ms");
        }

        private void Clear()
        {
            _particles.Clear();
            _particleVisuals.Clear();
        }

    
  
        
        SpatialPartitioningGrid<int> CreateSpatialPartitioningGrid()
        {
            float squareSize = 3.5f * Settings.Scale;
            return new SpatialPartitioningGrid<int>(Settings.AreaBounds, squareSize, MaxNumParticlesInPartitioningSquare);
        }

        void DoSpatialPartitioning(SpatialPartitioningGrid<int> grid, Particles particles)
        {
            grid.Clear();
            for (int i = 0; i < particles.NumParticles; i++)
            {
                grid.Add(i, particles[i].Position);
            }
        }


    }
}


