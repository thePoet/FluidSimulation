using UnityEngine;

namespace FluidDemo
{
    public class DebugDataVisualization : MonoBehaviour
    {
        private FluidSimDemo _fluidSimDemo;

        void Awake()
        {
            _fluidSimDemo = FindObjectOfType<FluidSimDemo>();    
        }


        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.I)) return;
            
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int[] particles = _fluidSimDemo.InsideCircle(mousePos, 15f);
            if (particles.Length > 0)
            {
                _fluidSimDemo.SelectDebugParticle(particles[0]);
            }
        }

        void OnDrawGizmos()
        {
            if (_fluidSimDemo == null) return;
            var data = _fluidSimDemo.ParticleDebugData();
            if (data == null) return;

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

    }
}