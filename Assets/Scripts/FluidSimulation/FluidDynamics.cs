using System;
using FluidSimulation.Internal;
using UnityEngine;

namespace FluidSimulation
{
    public enum FluidSubstance
    {
        SomeLiquid,
        SomeGas,
        SomeSolid
    }
    
    public class FluidDynamics : MonoBehaviour
    {
        public FluidParticles Particles; //TODO: readonly  

        private ShaderManager ShaderManager;
        // private SpatialPartitioningGrid<int> _partitioningGrid;
        private ParticleVisualization _particleVisualization;
        private LevelOutline _levelOutline;
        private bool _isPaused;
        private int _selectedParticle = -1;
        
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

   
        
            
            
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        private void Awake()
        {
            SetMaxFrameRate(60);

            _particleVisualization = FindObjectOfType<ParticleVisualization>();
            _levelOutline = FindObjectOfType<LevelOutline>();
            if (_particleVisualization == null) Debug.LogError("No visualization found in the scene.");
            if (_levelOutline == null) Debug.LogError("No container found in the scene.");

            ShaderManager = new ShaderManager("FluidDynamicsComputeShader", Settings, Fluids);


            var partitioningGrid = new SpatialPartitioningGrid<int>(
                Settings.PartitioningGrid,
                Settings.MaxNumParticlesInPartitioningCell,
                i => Particles.Get(i).Position);

            Particles = new FluidParticles(Settings.MaxNumParticles, partitioningGrid);

            void SetMaxFrameRate(int frameRate)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = frameRate;
            }
            
        }

        private void Start()
        {
            CreateWalls();
        }


        private void OnDestroy()
        {
            ShaderManager.Dispose();
        }

        void Update()
        {
            if (!_isPaused)
            {
                ShaderManager.Step(0.015f, Particles, Particles.NumParticles);
                UpdateParticleVisualization();
            }
            ProcessUserInput();

        }
        
        void OnDrawGizmos()
        {
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
            Gizmos.DrawLine(data[0], data[0] + data[1]*100f);
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private void ProcessUserInput()
        {
            if (Input.GetKeyDown(KeyCode.C)) Clear();
            if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused;
            //if (Input.GetKeyDown(KeyCode.T)) RunPerformanceTest();
        }

        public int SpawnParticle(Vector2 position, Vector2 velocity, FluidSubstance substance)
        {
            var particle = new FluidParticle()
            {
                Position = position,
                Velocity = velocity,
                FluidIndex = FluidIndex(substance)
            };

            int particleId = Particles.Add(particle);
            _particleVisualization.AddParticle(particleId, substance, position);

            return particleId;
        }

        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in Particles.InsideCircle(position, radius))
            {
                Particles.Particles[particleIdx].Velocity = velocity;
            }
        }
        

        

        public int[] ParticleIdsInsideCircle(Vector2 position, float radius) => Particles.InsideCircle(position, radius);
      
        public void SelectParticle(int particleId)
        {
            ShaderManager.SelectedParticle = particleId;
        }
        
        

        
        private void CreateWalls()
        {

        	Rect area = Settings.AreaBounds;

            // Find all monobehaviours in scene:
            var walls = GameObject.FindObjectsOfType<TestWallCreator>();

            foreach (var wall in walls)
            {
                float hSpacing = Mathf.Sqrt(3f) * wall.spacing;
                float vSpacing = (3f / 2f) * wall.spacing;

                for (int y = 0; y < wall.layersY; y++)
                {
                    for (int x = 0; x < wall.layersX; x++)
                    {
                        Vector2 localPos = new Vector2(x * hSpacing, y * vSpacing);
                        if (y%2 == 1) localPos += new Vector2(hSpacing/2f, 0f);
                        
                        Vector3 worldPos = wall.transform.TransformPoint(localPos);
                        SpawnParticle(worldPos, Vector2.zero, FluidSubstance.SomeSolid);
                    }
                    
                }
            }

        }
        
        private void UpdateParticleVisualization()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var particle in Particles.Particles)
            {
                _particleVisualization.UpdateParticle(particle.Id, particle.Position);
               _particleVisualization.ColorParticle(particle.Id, Color.blue);
            }
        }
        

        private void Clear()
        {
            Particles.Clear();
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
        
        
        #endregion

   
    }
}