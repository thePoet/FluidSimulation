using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FluidSimulation
{
    public class ParticleBrush : MonoBehaviour
    {
        public int blobsPerFrame = 1;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;

        [FormerlySerializedAs("simulation")] public TestFluidDynamics testFluidDynamics;

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
                for (int i=0; i<blobsPerFrame; i++)
                {
                  testFluidDynamics.SpawnParticle(MousePosition + RandomOffset, Velocity, FluidSubstance.SomeLiquid);
                }
            }

            if (RightMouseButton)
            {
                /*
                for (int i=0; i<blobsPerFrame; i++)
                {
                    testFluidDynamics.SpawnParticle(MousePosition + RandomOffset, Velocity, FluidSubstance.SomeSolid);
                }*/
                testFluidDynamics.MoveParticles(MousePosition, 15f, Vector2.up*1000f);
            }
        }

        bool LeftMouseButton => Input.GetMouseButton(0);
        bool RightMouseButton => Input.GetMouseButtonDown(1);
       
        Vector2 RandomOffset => Random.insideUnitCircle * brushRadius;
        
        Vector2 Velocity => Vector2.down * maxSpeed;
        
        Vector2 MousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

        
     


        
        
    }
}
