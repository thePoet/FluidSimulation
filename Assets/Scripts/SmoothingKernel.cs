
using UnityEngine;

namespace FluidSimulation
{
    /*
    public interface ISmoothingKernel
    {
        float Value(float distance);
        float Gradient(float distance);
        float Laplacian(float distance);
    }*/



    // https://www.diva-portal.org/smash/get/diva2:573583/FULLTEXT01.pdf

// normalized

    
    public class SmoothingKernel2D
    {
        float _d;

        delegate float KernelFunction(float r);
        
        public SmoothingKernel2D(float coreRadius)
        {
            _d = coreRadius;
        }

        // Poly(r,d) = 4/(π*d^8) * (d^2 - r^2)^3 assuming 0 ≤ r ≤ d otherwise 0
        // Notice the slight difference from the 3d version of the formula.
        // 3d version is presented in https://matthias-research.github.io/pages/publications/sca03.pdf
        public float Poly(float r)
        {
            if (_d < r || r < 0f) return 0f;
            float a = 4f / (Mathf.PI * Mathf.Pow(_d, 8f));
            float b = _d * _d - r * r;
            return a * b * b * b;
        }

        // PolyGradient(r,d) = -24/π*d^8 * r * (h^2 - r^2)^2 assuming 0 ≤ r ≤ d otherwise 0
        public float PolyGradient(float r)
        {
            if (_d < r || r < 0f) return 0f;
            float a = 24f / (Mathf.PI * Mathf.Pow(_d, 8f));
            float b = _d * _d - r * r;
            return -a * r * b * b;
        }



        //   (d - r)^2 / (π*d^4)/6  
        public float Spiky(float r)
        {
            if (_d < r || r < 0f) return 0f;
            
            float a = Mathf.PI * Mathf.Pow(_d, 4f)/6f;
            float b = _d - r;

            return b * b / a;
        }


        //   -12*(d-r) / π*d^4  
        public Vector2 SpikyGradient(Vector2 offsetFromCenter)
        {
            float r = offsetFromCenter.magnitude;
            if (_d < r || r < 0f) return Vector2.zero;
            float a = -12f / (Mathf.Pow(_d, 4f) * Mathf.PI);
            return offsetFromCenter.normalized * (_d - r) * a;
        }
        
        
        public void Test()
        {
            Debug.Log("TESTING KERNELS");
            Debug.Log("Poly normalized: " + Integrate(Poly));
            Debug.Log("Spiky normalized: " + Integrate(Spiky));/*
            Debug.Log("Poly grad " + PolyGradient(2.3f) + " " + Derivate(Poly, 2.3f));
            Debug.Log("Poly grad " + PolyGradient(0.7f) + " " + Derivate(Poly, 0.7f));
            Debug.Log("Spiky grad " + SpikyGradient(2.3f) + " " + Derivate(Spiky, 2.3f));
            Debug.Log("Spiky grad " + SpikyGradient(0.7f) + " " + Derivate(Spiky, 0.7f));

            */
            float Integrate(KernelFunction kernel)
            {
                float sum = 0f;
                float step = _d/100f;
                for (float x = -_d; x <= _d; x += step)
                {
                    for (float y = -_d; y <= _d; y += step)
                    {
                        float r = new Vector2(x,y).magnitude;
                            
                        sum += kernel(r) * step*step; 
                    }
                    
                }

                return sum;
            }
            
            float Derivate(KernelFunction kernel, float x)
            {
                float h = 0.001f;
                return (kernel(x + h) - kernel(x - h)) / (2f * h);
            }
          
            
            
            
        }

    }


}
