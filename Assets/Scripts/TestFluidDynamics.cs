using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FluidSimulation
{
    public enum FluidSubstance
    {
        SomeLiquid,
        SomeSolid
    }
    
    public class TestFluidDynamics : MonoBehaviour
    {
        public TMPro.TextMeshPro text;
        
     
    
        private FluidDynamics _fluidDynamics;
        private ParticleVisualization _particleVisualization;
        private Container _container;

        private bool _isPaused;
        
        private SimulationSettings SimulationSettings => new()
        {
            InteractionRadius = 15f,
            Gravity = 1200f,
            MaxNumParticles = 13000,
            IsViscosityEnabled = true,
            NumDensityDisplacementRounds = 2,
            AreaBounds = new Rect(Vector2.zero, new Vector2(700f, 400f)),
            MaxNumParticlesInPartitioningCell = 50,
            MaxNumNeighbours = 50
        };

        private Fluid[] Fluids => new[]
        {
            new Fluid
            {
                State = State.Liquid,
                Stiffness = 750f,
                NearStiffness = 1500f,
                RestDensity = 5f,
                ViscositySigma = 0.05f,
                ViscosityBeta = 0.05f,
                GravityScale = 1f,
                Mass = 1f
            },
            new Fluid
            {
                State = State.Gas,
                Stiffness = 200f,
                NearStiffness = 400f,
                RestDensity = 0.5f,
                ViscositySigma = 0.05f,
                ViscosityBeta = 0.05f,
                GravityScale = 0.0f,
                Mass = 0.05f
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

            text.text = "Particles: " + _fluidDynamics.Particles.Length;
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
                FluidSubstance.SomeSolid => 1,
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