using System;
using System.Collections;
using System.Collections.Generic;
using FluidSimulation;
using UnityEngine;


namespace FluidSimulation
{
   public class SimulationManager : MonoBehaviour
   {
       private Simulation _simulation;
       private Visualization _visualization; 
       
       private void Awake()
       {
           SetFrameRate(60);
           _simulation = CreateSimulation();
           
           _visualization = FindObjectOfType<Visualization>();
           if (_visualization == null) Debug.LogError("No visualization found in the scene.");
           
           void SetFrameRate(int frameRate)
           {
               QualitySettings.vSyncCount = 0;
               Application.targetFrameRate = frameRate;
           }
       }

       private Simulation CreateSimulation()
       {
           var container = FindObjectOfType<Container>();
           if (container == null) Debug.LogError("No container found in the scene.");
           var simulation = new Simulation(DefaultSettings, container);
           return simulation;
       }

       void Start()
        {

        }


        void Update()
        {
            _simulation.Step(Time.deltaTime);
            
         
            
            /*
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("Number of particles: " + _particleData.NumberOfParticles);
                Debug.Log(_particleData._partitioning.DebugInfo());
                // Debug.Log(_particleData.NeighbourhoodWatch());
            }

            if (Input.GetKeyDown(KeyCode.Space)) isRunning = !isRunning;

            if (Input.GetKeyDown(KeyCode.T))
            {
                CreatePerfTestParticles();
                perfTestTimer.Reset();
                perfTestTimerUpdate.Reset();
                
                perfTestCounter = 0;
                perfTestRunning = true;
                perfTestUpdateTime = 0f;
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                CreatePerfTestParticles();
                perfTestTimer.Reset();

                for (int i = 0; i < 60; i++)
                {
                    Step(0.015f);
                }
                
                Debug.LogFormat("PERF TEST WITHOUT DRAWING TOOK " + perfTestTimer.Time * 1000f + " ms");
          
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                _visualization.Clear();
                _particleData.Clear();
            }
            
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Application.Quit();
            }
*/

            UpdateVisuals();

        }



        public void SpawnParticle(Vector2 position, Vector2 velocity, ParticleType type)
        {
            int particleId = _simulation.SpawnParticle(position, velocity, type);
            _visualization.AddParticle(particleId, type);
        }
        
        private void UpdateVisuals()
        {
            foreach (var particle in _simulation.AllParticles())
            {
                _visualization.MoveParticle(particle.Id, particle.Position );
            }
        }

        Simulation.Settings DefaultSettings => new Simulation.Settings
        {
            interactionRadius = 15f,
            gravity = 500,
            restDensity = 5,
            stiffness = 750,
            nearStiffness = 1500,
            viscositySigma = 0f,
            viscosityBeta = 0.5f,
        };

    }
}