using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FluidSimulation
{
    public class ParticleBrush : MonoBehaviour
    {
        [FormerlySerializedAs("blobsPerFrame")] public int particlesPerFrame = 1;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;
        public TestFluidDynamics testFluidDynamics;
        
        private Vector2 _previousMousePosition;
        private FluidSubstance _currentSubstance = FluidSubstance.SomeLiquid;
        
        private void Start()
        {
            testFluidDynamics = FindObjectOfType<TestFluidDynamics>();
            if (testFluidDynamics == null) Debug.LogError("No TestFluidDynamics found in the scene.");
            
            
        }


        void Update()
        {
            if (testFluidDynamics == null) return;
            
            if (LeftMouseButton)
            {
                int amount = particlesPerFrame;
                if (_currentSubstance == FluidSubstance.SomeSolid) amount = 1;
                for (int i=0; i < amount; i++)
                {
                  testFluidDynamics.SpawnParticle(MousePosition + RandomOffset, Velocity, _currentSubstance);
                }
            }

            if (RightMouseButton)
            {
                Vector2 deltaMousePosition = MousePosition - _previousMousePosition;
                testFluidDynamics.SetParticleVelocities(MousePosition, 15f, deltaMousePosition/Time.deltaTime);
            }
            
            if (Input.GetKey(KeyCode.Alpha1)) _currentSubstance = FluidSubstance.SomeLiquid;
            if (Input.GetKey(KeyCode.Alpha2)) _currentSubstance = FluidSubstance.SomeGas;
            if (Input.GetKey(KeyCode.Alpha3)) _currentSubstance = FluidSubstance.SomeSolid;


            _previousMousePosition = MousePosition;
        }

        bool LeftMouseButton => Input.GetMouseButton(0);
        bool RightMouseButton => Input.GetMouseButton(1);
       
        Vector2 RandomOffset => Random.insideUnitCircle * brushRadius;
        
        Vector2 Velocity => Vector2.down * maxSpeed;
        
        Vector2 MousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

        
     


        
        
    }
}
