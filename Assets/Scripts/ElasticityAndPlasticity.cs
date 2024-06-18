
using UnityEngine;

public class ElasticityAndPlasticity : MonoBehaviour
{/*
        


        Tested Values:
        Plasticity = 5f,
        YieldRatio = 0.01f,
        SpringK = 1000f


        private void ApplyElasticityAndPlasticity(IParticleData particleData, float timeStep)
        {
            var particles = particleData.All();
            
            var interactionRadius = _settings.InteractionRadius;
            var alpha = _settings.Plasticity;
            var gamma = _settings.YieldRatio;

            // Adjust spring rest lengths
            for (int i = 0; i < particles.Length; i++)
            {
                foreach (int j in particleData.NeighbourIndices(i))
                {
                    if (i <= j) continue;
                    
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
 
                    if (!particleData.Springs.TryGetValue((i, j), out float restLenght))
                    {
                        // The Clavet et al. paper says that rest length of the spring should be set to
                        // interaction radius, but I found that it works better if it's set to the actual distance 
                        // between the particles.
                        particleData.Springs[(i, j)] = distance; 
                    }
                    else
                    {
                        float d = gamma * restLenght; // tolerable deformation
                        if (distance > restLenght + d)
                        {
                            particleData.Springs[(i, j)] = restLenght + timeStep * alpha * (distance - restLenght - d);
                        }
                        else if (distance < restLenght - d)
                        {
                            particleData.Springs[(i, j)] = restLenght - timeStep * alpha * (restLenght - d - distance);
                        }
                    }
                }
            }
            
         
            foreach (var spring in particleData.Springs)
            {
                // Remove dysfunctional springs
                if (spring.Value > interactionRadius || 
                    spring.Key.Item1 >= particleData.NumberOfParticles || 
                    spring.Key.Item2 >= particleData.NumberOfParticles)
                {
                    particleData.Springs.Remove(spring.Key);
                }
                
                // Apply spring displacements
                Vector2 iPos = particles[spring.Key.Item1].Position;
                Vector2 jPos = particles[spring.Key.Item2].Position;

                Vector2 displacement = Pow2(timeStep) * _settings.SpringK * (1f - spring.Value / interactionRadius) *
                                      (spring.Value - (jPos-iPos).magnitude) * (jPos-iPos).normalized;
                
                particles[spring.Key.Item1].Position -= displacement * 0.5f;
                particles[spring.Key.Item2].Position += displacement * 0.5f;
                                      

            }
            
            
            

        }*/
}
