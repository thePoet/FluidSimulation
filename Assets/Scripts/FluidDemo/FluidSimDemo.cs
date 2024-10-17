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
        
        private ParticleVisualization _particleVisualization;
        private LevelOutline _levelOutline;
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

        private void Awake()
        {
            SetMaxFrameRate(60);

            _particleVisualization = FindObjectOfType<ParticleVisualization>();
            _levelOutline = FindObjectOfType<LevelOutline>();
            if (_particleVisualization == null) Debug.LogError("No visualization found in the scene.");
            if (_levelOutline == null) Debug.LogError("No container found in the scene.");

            var alerts = CreateProximityAlertSubscriptions();

            _fluidDynamics = new FluidDynamics(Settings, Fluids.List, alerts);
            
            var partitioningGrid = new SpatialPartitioningGrid<int>(
                new Grid2D(Settings.AreaBounds, squareSize: 3.5f * Settings.Scale),
                40,
                i => _particles[i].Position);
            
            _particles = new Particles(Settings.MaxNumParticles, partitioningGrid);
            
            _avgUpdateTime = new MovingAverage(300);
        }

        void Update()
        {
            float t = Time.realtimeSinceStartup;
            
            if (!_isPaused)
            {
                _fluidDynamics.Step(0.015f, _particles.FluidDynamicsParticles, _particles.NumParticles);
                _particles.UpdateSpatialPartitioningGrid();
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

        void OnDrawGizmos()
        {
            if (_fluidDynamics == null) return;
            var data = _fluidDynamics.DebugData();
            if (data == null) return;

            Gizmos.DrawSphere(data[0], 5f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(data[0], data[0] + data[2]*100f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(data[0], data[0] + data[3]*100f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(data[0], data[0] + data[4]*100f);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(data[0], data[0] + data[1]*100f);
        }

    

        public int SpawnParticle(Vector2 position, Vector2 velocity, FluidId fluidId)
        {
            var particle = new Particle();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.SetFluid(fluidId);

            int particleId = _particles.Add(particle);
            _particleVisualization.AddParticle(particleId, fluidId, position);

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
            
            _particleVisualization.RemoveParticle(particleIdx);
            _particleVisualization.AddParticle(particleIdx, fluidId,_particles[particleIdx].Position);
        }


        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in _particles.InsideCircle(position, radius))
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
            if (Input.GetKeyDown(KeyCode.I)) SelectDebugParticle();
        }

        private void UpdateParticleVisualization()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var particle in _particles.Span)
            {
                _particleVisualization.UpdateParticle(particle.Id, particle.Position);
            }
            
            t= Time.realtimeSinceStartup-t;
            
//            Debug.Log("Updating particle visualization took" + t*1000f + " ms");
        }

        private void Clear()
        {
            _particles.Clear();
            _particleVisualization.Clear();
        }

        private void SelectDebugParticle()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int[] particles = _particles.InsideCircle(mousePos, 15f);
            if (particles.Length > 0)
            {
                _fluidDynamics.SubscribeDebugData(particles[0]);
            }
        }
        
        private void SetMaxFrameRate(int frameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        }


    }
}


