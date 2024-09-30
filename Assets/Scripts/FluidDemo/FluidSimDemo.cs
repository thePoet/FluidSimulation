using UnityEngine;
using FluidSimulation;
using FluidSimulation.Internal;  // TODO: POIS!!

namespace FluidDemo
{
    public enum FluidSubstance
    {
        SomeLiquid,
        SomeGas,
        SomeSolid
    }

    public class FluidSimDemo : MonoBehaviour
    {


        private FluidDynamics _fluidDynamics;
        private ParticleVisualization _particleVisualization;
        private LevelOutline _levelOutline;
        private bool _isPaused;

        private SimulationSettings Settings => new()
        {
            InteractionRadius = 20f,
            Gravity = 1200f,
            Drag = 0.001f,
            MaxNumParticles = 30000,
            IsViscosityEnabled = true,
            NumSubSteps = 3,
            AreaBounds = new Rect(Vector2.zero, new Vector2(1200f, 600f)),
            MaxNumParticlesInPartitioningCell = 100,
            MaxNumNeighbours = 50
        };

        private FluidInternal[] Fluids => new[]
        {
            new FluidInternal
            {
                State = State.Liquid,
                Stiffness = 2000f,
                NearStiffness = 4000f,
                RestDensity = 5f,
                ViscositySigma = 0.01f,
                ViscosityBeta = 0.01f,
                GravityScale = 1f,
                Mass = 1f,
                DensityPullFactor = 0.5f
            },
            new FluidInternal
            {
                State = State.Gas,
                Stiffness = 200f,
                NearStiffness = 400f,
                RestDensity = 1f,
                ViscositySigma = 0.05f,
                ViscosityBeta = 0.05f,
                GravityScale = -0.05f,
                Mass = 0.1f,
                DensityPullFactor = 1f
            },
            new FluidInternal
            {
                State = State.Solid,
                Stiffness = 1f,
                NearStiffness = 1f,
                RestDensity = 1f,
                ViscositySigma = 0f,
                ViscosityBeta = 0f,
                GravityScale = 0.0f,
                Mass = 1f,
                DensityPullFactor = 0.0f
            }
        };

        private void Awake()
        {
            SetMaxFrameRate(60);

            _particleVisualization = FindObjectOfType<ParticleVisualization>();
            _levelOutline = FindObjectOfType<LevelOutline>();
            if (_particleVisualization == null) Debug.LogError("No visualization found in the scene.");
            if (_levelOutline == null) Debug.LogError("No container found in the scene.");


            _fluidDynamics = new FluidDynamics(Settings, Fluids);

            void SetMaxFrameRate(int frameRate)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = frameRate;
            }

        }



        // Start is called before the first frame update
        void Start()
        {

        }



        void OnDrawGizmos()
        {
            /*
            if (ShaderManager == null) return;
            var data = ShaderManager.GetSelectedParticleData();
            if (data==null) return;

            Gizmos.DrawSphere(data[0], 5f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(data[0], data[0] + data[2]*100f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(data[0], data[0] + data[3]*100f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(data[0], data[0] + data[4]*100f);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(data[0], data[0] + data[1]*100f);*/
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
        
        public int SpawnParticle(Vector2 position, Vector2 velocity, FluidSubstance substance)
        {
            var particle = new FluidParticle()
            {
                Position = position,
                Velocity = velocity,
                FluidIndex = FluidIndex(substance)
            };

            int particleId = _fluidDynamics.Particles.Add(particle);
            _particleVisualization.AddParticle(particleId, substance, position);

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
            //if (Input.GetKeyDown(KeyCode.T)) RunPerformanceTest();
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
        
        private int FluidIndex(FluidSubstance substance) 
        {
            return substance switch
            {
                FluidSubstance.SomeLiquid => 0,
                FluidSubstance.SomeGas => 1,
                FluidSubstance.SomeSolid => 2,
                _ => throw new System.ArgumentOutOfRangeException(nameof(substance), substance, null)
            };
        }



    }
}


