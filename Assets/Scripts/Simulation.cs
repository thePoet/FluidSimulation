using System.Collections;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace FluidSimulation
{
    public class Simulation : MonoBehaviour
    {
        public ParticleDynamics.Settings settings;
        private IParticleData _particleData;
        private ParticleDynamics _particleDynamics;
        private ParticleVisualization _particleVisualization;
        private Container _container;

        private bool _isPaused;

        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        private void Awake()
        {
            SetMaxFrameRate(60);

            _particleVisualization = FindObjectOfType<ParticleVisualization>();
            _container = FindObjectOfType<Container>();
            if (_particleVisualization == null) Debug.LogError("No visualization found in the scene.");
            if (_container == null) Debug.LogError("No container found in the scene.");


            _particleDynamics =  new ParticleDynamics(settings, _container.Bounds);

            _particleData = new ParticleData(
                maxNumParticles: 10000, 
                maxNumNeighbours: 100, 
                DefaultSettings.InteractionRadius, 
                _container.Bounds
            );
            
            void SetMaxFrameRate(int frameRate)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = frameRate;
            }
        }

        void Update()
        {
            if (!_isPaused)
            {
                _particleDynamics.Step(_particleData, 0.015f);
                UpdateParticleVisualization();
            }
            ProcessUserInput();
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private void ProcessUserInput()
        {
            if (Input.GetKeyDown(KeyCode.C)) Clear();
            if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();
            if (Input.GetKeyDown(KeyCode.N)) Debug.Log("Number of particles: " + _particleData.NumberOfParticles);
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused;
            if (Input.GetKeyDown(KeyCode.T)) RunPerformanceTest();
        }

        public int SpawnParticle(Vector2 position, Vector2 velocity, ParticleType type)
        {
            var particle = new FluidParticle()
            {
                Position = position,
                Velocity = velocity,
                Type = type
            };

            int particleId = _particleData.Add(particle);
            _particleVisualization.AddParticle(particleId, type);

            return particleId;
        }
        
        public void MoveParticle(int particleId, Vector2 newPosition)
        {
            _particleData.All()[particleId].Position = newPosition;
        }

      

        private void UpdateParticleVisualization()
        {
            foreach (var particle in _particleData.All())
            {
                _particleVisualization.MoveParticle(particle.Id, particle.Position);
            }
        }

        private void Clear()
        {
            _particleData.Clear();
            _particleVisualization.Clear();
        }

        private void RunPerformanceTest()
        {
            Clear();
            
            Random.InitState(123);
         
            for (int i = 0; i < 500; i++)
            {
                SpawnParticle(RandomPosition(), Vector2.zero, ParticleType.Liquid);
            }

            Timer timer = new Timer();
            for (int i = 0; i < 60; i++) _particleDynamics.Step(_particleData, 0.015f);
            Debug.Log("Performance test took " + timer.Time * 1000f + " ms.");
            
            Vector2 RandomPosition()
            {
                return new Vector2
                (
                    x: Random.Range(_container.Bounds.xMin, _container.Bounds.xMax),
                    y: Random.Range(_container.Bounds.yMin, _container.Bounds.yMax)
                );
            }
        }

        ParticleDynamics.Settings DefaultSettings => new ParticleDynamics.Settings
        {
            InteractionRadius = 15f,
            Gravity = 500,
            RestDensity = 5,
            Stiffness = 750,
            NearStiffness = 1500,
            ViscositySigma = 0f,
            ViscosityBeta = 0.05f,
            AreElasticityAndPlasticityEnabled = false
        };
        
        ParticleDynamics.Settings PoopSettings => new ParticleDynamics.Settings
        {
            InteractionRadius = 15f,
            Gravity = 500,
            RestDensity = 5,
            Stiffness = 750,
            NearStiffness = 1500,
            ViscositySigma = 0f,
            ViscosityBeta = 0.4f,
            AreElasticityAndPlasticityEnabled = true,
            Plasticity = 5f,
            YieldRatio = 0.01f,
            SpringK = 1000f
        };
        
        #endregion
    }
}