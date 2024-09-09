using UnityEngine;


namespace FluidSimulation
{
    public enum FluidSubstance
    {
        SomeLiquid,
        SomeGas,
        SomeSolid
    }
    
    public class TestFluidDynamics : MonoBehaviour
    {
        public TMPro.TextMeshPro text;

        private FluidDynamics _fluidDynamics;
        private ParticleVisualization _particleVisualization;
        private Container _container;

        private bool _isPaused;

        private int _selectedParticle = 0;
        
        private SimulationSettings SimulationSettings => new()
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
                ViscositySigma = 0.02f,
                ViscosityBeta = 0.02f,
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

            _fluidDynamics =  new FluidDynamics(SimulationSettings, Fluids);
            
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
            _fluidDynamics.EndSimulation();
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
        
        void OnDrawGizmos()
        {
            if (_fluidDynamics == null) return;
            var data = _fluidDynamics._computeShader.GetSelectedParticleData();
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

            int particleId = _fluidDynamics.AddParticle(particle);
            _particleVisualization.AddParticle(particleId, substance);

            return particleId;
        }

        public void SetParticleVelocities(Vector2 position, float radius, Vector2 velocity)
        {
            foreach (var particleIdx in _fluidDynamics.ParticlesInsideCircle(position, radius))
            {
                _fluidDynamics.Particles[particleIdx].Velocity = velocity;
            }
        }

        public int[] ParticleIdsInsideCircle(Vector2 position, float radius) => _fluidDynamics.ParticlesInsideCircle(position, radius);
      
        public void SelectParticle(int particleId)
        {
            _fluidDynamics.SelectParticle(particleId);
        }
        
        private void CreateWalls()
        {

        	Rect area = SimulationSettings.AreaBounds;

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
            /*
            float d = 2f;
            float depth = 5f;
            float margin = 35f;


            for (float x = area.min.x+margin; x < area.max.x-margin; x+=d)
            {
                float offset = UnityEngine.Random.Range(0f, depth);
                SpawnParticle(new Vector2(x, area.min.y+margin+offset), Vector2.zero, FluidSubstance.SomeSolid);
                SpawnParticle(new Vector2(x, area.max.y-margin-offset), Vector2.zero, FluidSubstance.SomeSolid);
            }
            for (float y = area.min.y+margin; y < area.max.y-margin; y+=d)
            {
                float offset = UnityEngine.Random.Range(0f, depth);
                SpawnParticle(new Vector2(area.min.x+margin+offset, y), Vector2.zero, FluidSubstance.SomeSolid);
                SpawnParticle(new Vector2(area.max.x-margin-offset, y), Vector2.zero, FluidSubstance.SomeSolid);
            }
            */
        }

  

        private void UpdateParticleVisualization()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var particle in _fluidDynamics.Particles)
            {
                _particleVisualization.UpdateParticle(particle.Id, particle.Position);
               _particleVisualization.ColorParticle(particle.Id, Color.blue);
               // _particleVisualization.ColorParticle(particle.Id, particle.color);
            }

        }

   

        private void Clear()
        {
            _fluidDynamics.Clear();
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