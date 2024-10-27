using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FluidDemo
{
    public class ParticleBrush : MonoBehaviour
    {
        public int particlesPerFrame = 1;
        public bool oneAtTime = false;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;
        public Simulation simulation;
        
        private Vector2 _previousMousePosition;
        private FluidId _currentFluidId = FluidId.Water;
        
        private void Start()
        {
            simulation = FindObjectOfType<Simulation>();
            if (simulation == null) Debug.LogError("No simulation found in the scene.");
        }

        void Update()
        {
            if (simulation is null) return;
            
            if (LeftMouseButton)
            {
                int amount = particlesPerFrame;

                if (oneAtTime || _currentFluidId == FluidId.Rock )
                {
                    amount = 1;
                    if (!Input.GetMouseButtonDown(0)) return;
                }
        
                for (int i=0; i < amount; i++)
                { 
                    simulation.SpawnParticle(MousePosition + RandomOffset, Velocity, _currentFluidId);
                }
            }

            if (RightMouseButton)
            {
                Vector2 deltaMousePosition = MousePosition - _previousMousePosition;
                Vector2 velocity = deltaMousePosition/Time.deltaTime;
        
                foreach (var id in ParticlesInBrush)
                {
                    var particle = simulation.GetParticle(id);
                    //particle.Velocity = velocity;
                    //simulation.UpdateParticle(particle);
                    simulation.DestroyParticle(id);
                }
            }
  
            
            if (Input.GetKey(KeyCode.Alpha1)) _currentFluidId = FluidId.Water;
            if (Input.GetKey(KeyCode.Alpha2)) _currentFluidId = FluidId.Smoke;
            if (Input.GetKey(KeyCode.Alpha3)) _currentFluidId = FluidId.Rock;
            if (Input.GetKey(KeyCode.Alpha4)) _currentFluidId = FluidId.GreenLiquid;
            if (Input.GetKey(KeyCode.Alpha5)) _currentFluidId = FluidId.RedLiquid;


            _previousMousePosition = MousePosition;
        }
        
    
        
        private ParticleId[] ParticlesInBrush => simulation.ParticlesInsideCircle(MousePosition, 15f);
 

        bool LeftMouseButton => Input.GetMouseButton(0);
        bool RightMouseButton => Input.GetMouseButton(1);
       
        Vector2 RandomOffset => Random.insideUnitCircle * brushRadius;
        
        Vector2 Velocity => Vector2.down * maxSpeed;
        
        Vector2 MousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

        
     


        
        
    }
}
