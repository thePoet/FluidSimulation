
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsComputeShader
    {

        public FluidsComputeShader() //int maxNumberOfParticles, int maxNumNeighbours, float interactionRadius, Rect bounds)
        {
            var s = Resources.Load("FluidDynamicsComputeShader");
            Debug.Log(s);
        }

    }
}
