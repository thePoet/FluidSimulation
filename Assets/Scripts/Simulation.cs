using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Serialization;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;

// Based on paper by Miles Macklin and Matthias Müller
// https://mmacklin.com/pbf_sig_preprint.pdf

// useful reference: https://github.com/yuki-koyama/position-based-fluids/blob/main/src/main.cpp
namespace FluidSimulation
{
    
    
    public class Simulation : MonoBehaviour
    {
 
        private static List<LiquidParticle> _particles;
  
      
        public int numSolverIterations;
        public float interactionDistance;
        public float restDensity;
        public float particleMass;
        public float relaxationParameter;
        public float gravity;
        public float tensileInstabilityCorrection = 0.1f;
        
        public GameObject liquidParticlePrefab;
        
        public float timeAll;
        public float timeNeighbours;
        
        private SmoothingKernel2D _smoothing;
        private MovingAverage timeAvgCalc;
        private MovingAverage timeAvgNeighbours;
        private MovingAverage timeAvgAll;
        private Timer timer;
        private Timer timerAll;

        private Texture2D densityTexture;
        private Container container;
        private bool isRunning;
        
      
        
        
        
        static Simulation()
        {
            _particles = new List<LiquidParticle>();
        }
        
        public static void AddParticle(LiquidParticle particle)
        {
            _particles.Add(particle);
        }
        
        public static List<LiquidParticle> GetLiquidParticlesInCircle(Vector2 center, float radius)
        {
            var colliders = Physics2D.OverlapCircleAll(center, radius);
            return colliders.Where(c => c.GetComponent<LiquidParticle>() != null)
                .Select(c => c.GetComponent<LiquidParticle>()).ToList();
        }

        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        private void Awake()
        {
            _smoothing = new SmoothingKernel2D(interactionDistance);
            _smoothing.Test();
            timeAvgNeighbours = new MovingAverage(50);
            timeAvgAll = new MovingAverage(50);
            timerAll = new Timer();
            timer = new Timer();

            densityTexture = new Texture2D(100, 100);
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    densityTexture.SetPixel(i, j, new Color(0f, 0f, 1f, 0.5f));
                }
            }
            densityTexture.Apply();

            container = GameObject.FindObjectOfType<Container>();

            isRunning = true;

        }

       
        
        private void Update()
        {
            if (isRunning || Input.GetKeyDown(KeyCode.Period)) Simulate(0.015f);
            
            
            if (Input.GetKeyDown(KeyCode.N)) Debug.Log("Number of particles: " + _particles.Count);
            if (Input.GetKeyDown(KeyCode.D))
            {
                // get mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Debug.Log("Density: " + DensityAt(mousePosition));
            }
            
            if (Input.GetKeyDown(KeyCode.Space)) isRunning = !isRunning;
            
            if (Input.GetKeyDown(KeyCode.T)) PerformanceTest();
        }
        
      

        void OnDrawGizmos()
        {

          DrawDensityMap();
          //DrawVelocityVectors();

          void DrawDensityMap()
          {
              for (int i = 0; i < 100; i++)
              {
                  for (int j = 0; j < 100; j++)
                  {
                      Vector2 position = container.Bounds.min + container.Bounds.size * new Vector2(i / 100f, j / 100f);
                      Color color = ColorForDensity(DensityAt(position));
                      densityTexture.SetPixel(i, 99-j, color);
                  }
              }
              densityTexture.Apply();
              Gizmos.DrawGUITexture(container.Bounds, densityTexture);
          }

        
          void DrawVelocityVectors()
          {
              foreach (var particle in _particles)
              {
                  Gizmos.DrawLine(particle.Position, particle.Position + particle.velocity * 0.3f);
              }
          }


          Color ColorForDensity(float density)
          {
              float margin = 0.1f;
              
              Color restDensityColor = new Color(0f, 0f, 1f, 0.5f);
              Color highDensityColor = new Color(1f, 0f, 0f, 0.5f);
              Color lowDensityColor = new Color(0f, 1f, 0f, 0.5f);
              
                if (density < restDensity * (1f - margin)) return lowDensityColor;
                if (density > restDensity * (1f + margin)) return highDensityColor;

                return restDensityColor;
          }
        }
        #endregion
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
          
        public void SpawnParticleAt(Vector2 position) => Instantiate(liquidParticlePrefab, position, Quaternion.identity);

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        void Simulate(float timeStep)
        {
            
            timerAll.Reset();
            
            _particles.ForEach(p => p.startPosition = p.Position);

            _particles.ForEach(p => ApplyExternalForcesTo(p, timeStep));
            _particles.ForEach(p => p.Position += p.velocity * timeStep);

            _particles.ForEach(p => p.neighbours = NeighboursOf(p));
           
            for (int i = 0; i < numSolverIterations; i++)
            {
                _particles.ForEach(p => p.ScalingFactor = ScalingFactor(p, p.neighbours));
                _particles.ForEach(p => p.Position += DeltaPosition(p));
                
                if (container!= null) ConfineParticlesInBox(_particles, container.Bounds);
            }
            
            _particles.ForEach(p => p.velocity = (p.Position - p.startPosition) / timeStep);
            //_particles.ForEach(p => p.Position = p.newPosition);
            
            timeAvgAll.Add(timerAll.Time);
            timeAll = timeAvgAll.Average() * 1000f;
            
            /*
            int sumNumNeighbours = 0;
            float sumDensity = 0f;
            foreach (var particle in _particles)
            {
                sumNumNeighbours += particle.neighbours.Count;
                sumDensity += DensityAt(particle.Position);
            }

            if (_particles.Count > 0)
            {
                Debug.Log("avg neighbours: " + sumNumNeighbours / _particles.Count);
                Debug.Log("avg density: " + sumDensity / _particles.Count);
            }*/
        }


        private void ApplyExternalForcesTo(LiquidParticle particle, float timeStep)
        {
            particle.velocity += Vector2.down * timeStep * gravity;
        }

        // Calculate the change in position for given particle
        // Equation 12 in Müller et al. 2003
        private Vector2 DeltaPosition(LiquidParticle particle)
        {
            Vector2 sum = Vector2.zero;
            foreach (var otherParticle in particle.neighbours)
            {
                Vector2 gradient = _smoothing.SpikyGradient(particle.Position - otherParticle.Position);
                
                // Tensile Instability correction (Eq. 13 in Müller et al. 2003)
                float term = _smoothing.Spiky((particle.Position - otherParticle.Position).magnitude)/_smoothing.Spiky(0.2f*interactionDistance);
                float correction = -tensileInstabilityCorrection * term * term * term * term;
                
                sum += (particle.ScalingFactor + otherParticle.ScalingFactor + correction) * gradient;
            }
            
            return 1f/restDensity * sum;
        }


    

        // 
        // Scaling factor λ for given particle
        // Equation 11 in Müller et al. 2003
        private float ScalingFactor(LiquidParticle particle, List<LiquidParticle> neighbours)
        {
            float densityConstraint =  DensityAt(particle.Position, neighbours) / restDensity - 1f;
            
          //  if (densityConstraint < 0f) return 0f;
            
            float sum = 0f;
            foreach (var otherParticle in neighbours)
            {
                sum += GradientOfConstraint(particle, otherParticle).sqrMagnitude;
            }
            
            return -densityConstraint /(sum + relaxationParameter);
        }
        
        
        private float DensityAt(Vector2 position, List<LiquidParticle> nearbyParticles = null)
        {
            if (nearbyParticles == null) nearbyParticles = GetLiquidParticlesInCircle(position, interactionDistance);
            
            float density = 0f;
            foreach (var particle in nearbyParticles)
            {
                float distance = (position - particle.Position).magnitude;
                density += _smoothing.Poly(distance) * particleMass;
            }
            return density;
        }
     
        
        // Gradient of constraint function for particle i with respect to particle k
        // Equation 8 in Müller et al. 2003
        private Vector2 GradientOfConstraint(LiquidParticle i, LiquidParticle k)
       {
            Vector2 result = Vector2.zero;

            if (i == k)
            {
                foreach (LiquidParticle j in i.neighbours)
                {
                    result += _smoothing.SpikyGradient(i.Position - j.Position);
                }
                return result / restDensity;
            }
            else
            {
                return -_smoothing.SpikyGradient( i.Position - k.Position) / restDensity;
            }
            
       }
        
        
        private List<LiquidParticle> NeighboursOf(LiquidParticle p) => GetLiquidParticlesInCircle(p.Position, interactionDistance);
        

     


        void ConfineParticlesInBox(List<LiquidParticle> particles, Rect boxBounds)
        {
            foreach (var particle in particles)
            {
                if (boxBounds.Contains(particle.Position)) continue;
                Vector2 clampedPosition = new Vector2(Mathf.Clamp(particle.Position.x, boxBounds.xMin, boxBounds.xMax),
                    Mathf.Clamp(particle.Position.y, boxBounds.yMin, boxBounds.yMax));
                particle.Position = clampedPosition + Random.insideUnitCircle * 0.1f;

            }
        }


        void PerformanceTest()
        {
            if (_particles.Count != 0) return;
            
            Random.InitState(123);
            
            for (int i = 0; i < 500; i++)
            {
                Vector2 position = new Vector2
                (
                    x: Random.Range( container.Bounds.xMin, container.Bounds.xMax),
                    y: Random.Range( container.Bounds.yMin, container.Bounds.yMax)
                );
          
                SpawnParticleAt(position);
            }
            
            Timer timer = new Timer();
            for (int i = 0; i < 60; i++)
            {
                Simulate(0.016666f);
            }
            
            Debug.Log("Performance test time: " + timer.Time*1000f + " ms");
            
        }

        
        #endregion
    }
}
