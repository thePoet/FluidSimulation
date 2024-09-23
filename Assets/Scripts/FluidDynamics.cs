using System;
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
        public int NumParticles { get; private set; }

        private FluidsShaderManager ShaderManager;
        private FluidParticle[] _particles;
        private int _nextId = 1;
        private SpatialPartitioningGrid<int> _partitioningGrid;
        private ParticleVisualization _particleVisualization;
        private Container _container;
        private bool _isPaused;
        private int _selectedParticle = -1;
        
        private SimulationSettings Settings => new()
        {
            InteractionRadius = 20f,
            Gravity = 1200f,
            Drag = 0.001f,
            MaxNumParticles = 13000,
            IsViscosityEnabled = true,
            NumSubSteps = 3,
            AreaBounds = new Rect(Vector2.zero, new Vector2(700f, 400f)),
            MaxNumParticlesInPartitioningCell = 50,
            MaxNumNeighbours = 50
        };

        private Fluid[] Fluids => new[]
        {
            new Fluid
            {
                State = State.Liquid,
                Stiffness = 2000f,
                NearStiffness = 4000f,
                RestDensity = 5f,
                ViscositySigma = 0.01f,
                ViscosityBeta = 0.01f,
                GravityScale = 1f,
                Mass = 1f
            },
            new Fluid
            {
                State = State.Gas,
                Stiffness = 300f,
                NearStiffness = 600f,
                RestDensity = 0.25f,
                ViscositySigma = 0.15f,
                ViscosityBeta = 0.15f,
                GravityScale = 0.0f,
                Mass = 0.01f
            },
            new Fluid
            {
                State = State.Solid,
                Stiffness = 1f,
                NearStiffness = 1f,
                RestDensity = 1f,
                ViscositySigma = 0f,
                ViscosityBeta = 0f,
                GravityScale = 0.0f,
                Mass = 1f
            }
        };

   
        
            
            
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        private void Awake()
        {
            SetMaxFrameRate(60);

            _particleVisualization = FindObjectOfType<ParticleVisualization>();
            _container = FindObjectOfType<Container>();
            if (_particleVisualization == null) Debug.LogError("No visualization found in the scene.");
            if (_container == null) Debug.LogError("No container found in the scene.");

            ShaderManager = new FluidsShaderManager("FluidDynamicsComputeShader", Settings, Fluids);
            _particles = new FluidParticle[Settings.MaxNumParticles];


            _partitioningGrid = new SpatialPartitioningGrid<int>(
                Settings.PartitioningGrid,
                Settings.MaxNumParticlesInPartitioningCell,
                i => _particles[i].Position);
            
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
                ShaderManager.Step(0.015f, _particles, NumParticles);
                UpdateSpatialPartitioningGrid();
                UpdateParticleVisualization();
            }
            ProcessUserInput();

        }
        
        void OnDrawGizmos()
        {
            if (_selectedParticle == -1) return;
            var data = ShaderManager.GetSelectedParticleData();
            Gizmos.DrawSphere(data[0], 5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(data[0], data[0] + data[1]);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(data[0], data[0] + data[2]*100f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(data[0], data[0] + data[3]);
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

            int particleId = AddParticle(particle);
            _particleVisualization.AddParticle(particleId, substance);

            return particleId;
        }

        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in ParticlesInsideCircle(position, radius))
            {
                Particles[particleIdx].Velocity = velocity;
            }
        }
        
        
        public int[] ParticlesInsideCircle(Vector2 position, float radius) => _partitioningGrid.CircleContents(position, radius);
        
        public int AddParticle(FluidParticle particle)
        {
            particle.Id = _nextId;
            _nextId++;
            NumParticles++;
            int index = NumParticles - 1;
            _particles[index] = particle;
            return particle.Id;
        }

        public int[] ParticleIdsInsideCircle(Vector2 position, float radius) => ParticlesInsideCircle(position, radius);
      
        public void SelectParticle(int particleId)
        {
            ShaderManager.SelectedParticle = particleId;
        }
        
        private Span<FluidParticle> Particles => _particles.AsSpan().Slice(0, NumParticles);

        
        // TODO: Read from compute buffer instead.
        private void UpdateSpatialPartitioningGrid()
        {
            _partitioningGrid.Clear();
      
            for (int i = 0; i < NumParticles; i++)
            {
                _partitioningGrid.Add(i);
            }
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
            foreach (var particle in Particles)
            {
                _particleVisualization.UpdateParticle(particle.Id, particle.Position);
               _particleVisualization.ColorParticle(particle.Id, Color.blue);
            }
        }
        

        private void Clear()
        {
            NumParticles = 0;
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
/*
        private void RunPerformanceTest()
        {
            Clear();
            
            Random.InitState(123);
         
            for (int i = 0; i < 4000; i++)
            {
                SpawnParticle(RandomPosition(), Vector2.zero, FluidSubstance.SomeLiquid);
            }

            Timer timer = new Timer();
            for (int i = 0; i < 60; i++) _fluidDynamics.Step(_particleData, 0.015f);
            Debug.Log("Performance test took " + timer.Time * 1000f + " ms.");
            
            Vector2 RandomPosition()
            {
                return new Vector2
                (
                    x: Random.Range(SimulationSettings.AreaBounds.xMin, SimulationSettings.AreaBounds.xMax),
                    y: Random.Range(SimulationSettings.AreaBounds.yMin, SimulationSettings.AreaBounds.yMax)
                );
            }
        }
*/

        
        #endregion

   
    }
}