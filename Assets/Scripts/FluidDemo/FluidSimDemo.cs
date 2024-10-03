using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public class FluidSimDemo : MonoBehaviour
    {
        private FluidDynamics _fluidDynamics;
        private ParticleVisualization _particleVisualization;
        private LevelOutline _levelOutline;
        private bool _isPaused;
    
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

            _fluidDynamics = new FluidDynamics(Settings, Fluids.List);

            void SetMaxFrameRate(int frameRate)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = frameRate;
            }
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

        void Update()
        {
            if (!_isPaused)
            {
                _fluidDynamics.Step(0.015f);
                UpdateParticleVisualization();
            }
            ProcessUserInput();
        }
        
        public int SpawnParticle(Vector2 position, Vector2 velocity, FluidId fluidId)
        {
            var particle = new FluidParticle();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.SetFluid(fluidId);

            int particleId = _fluidDynamics.Particles.Add(particle);
            _particleVisualization.AddParticle(particleId, fluidId, position);

            return particleId;
        }

        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in _fluidDynamics.Particles.InsideCircle(position, radius))
            {
                _fluidDynamics.Particles.Particles[particleIdx].Velocity = velocity;
            }
        }

        private void ProcessUserInput()
        {
            if (Input.GetKeyDown(KeyCode.C)) Clear();
            if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused;
            if (Input.GetKeyDown(KeyCode.I)) SelectDebugParticle();
        }

        private void UpdateParticleVisualization()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var particle in _fluidDynamics.Particles.Particles)
            {
                _particleVisualization.UpdateParticle(particle.Id, particle.Position);
                _particleVisualization.ColorParticle(particle.Id, Color.blue);
            }
        }

        private void Clear()
        {
            _fluidDynamics.Particles.Clear();
            _particleVisualization.Clear();
        }

        private void SelectDebugParticle()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int[] particles = _fluidDynamics.Particles.InsideCircle(mousePos, 15f);
            if (particles.Length > 0)
            {
                _fluidDynamics.SubscribeDebugData(particles[0]);
            }
        }

    }
}


