using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FluidSimulation
{
    public class ParticleBrush : MonoBehaviour
    {
        public int blobsPerFrame = 1;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;

        public Simulation simulation;        
        
        void Update()
        {
            if (LeftMouseButton)
            {
                for (int i=0; i<blobsPerFrame; i++)
                {
                  simulation.SpawnParticle(MousePosition + RandomOffset, Velocity, ParticleType.Liquid);
                }
            }

            if (RightMouseButton)
            {
                simulation.SpawnParticle(MousePosition + RandomOffset*0.25f, Velocity, ParticleType.Solid);
            }
        }

        bool LeftMouseButton => Input.GetMouseButton(0);
        bool RightMouseButton => Input.GetMouseButton(1);
       
        Vector2 RandomOffset => Random.insideUnitCircle * brushRadius;
        
        Vector2 Velocity => Vector2.down * maxSpeed;
        
        Vector2 MousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

        
     


        
        
    }
}
