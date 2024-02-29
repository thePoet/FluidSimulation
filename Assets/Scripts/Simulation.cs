using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;

// Based on paper by Miles Macklin and Matthias Müller
// https://mmacklin.com/pbf_sig_preprint.pdf

// useful reference: https://github.com/yuki-koyama/position-based-fluids/blob/main/src/main.cpp
namespace FluidSimulation
{


    public class Simulation : MonoBehaviour
    {
        private struct BoxEdge
        {
            public BoxEdge(Vector2 start, Vector2 end, Vector2 normal)
            {
                this.start = start;
                this.end = end;
                this.normal = normal;
            }
            public Vector2 start;
            public Vector2 end;
            public Vector2 normal;
        }

        private static List<LiquidParticle> _particles;


        public float interactionRadius;
        public float stiffness;
        public float nearStiffness;
        public float gravity;
        public float restDensity;

        public GameObject liquidParticlePrefab;


        private MovingAverage timeAvgCalc;
        private MovingAverage timeAvgAll;
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

            timeAvgAll = new MovingAverage(50);
            timerAll = new Timer();


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
            /*
            if (Input.GetKeyDown(KeyCode.D))
            {
                // get mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Debug.Log("Density: " + DensityAt(mousePosition));
            }*/

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
                        Vector2 position = container.Bounds.min +
                                           container.Bounds.size * new Vector2(i / 100f, j / 100f);
                        //     Color color = ColorForDensity(DensityAt(position));
                        //    densityTexture.SetPixel(i, 99-j, color);
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

        public void SpawnParticleAt(Vector2 position, Vector2 velocity = default)
        {
            GameObject particle = Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            particle.GetComponent<LiquidParticle>().velocity = velocity;
        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        void Simulate(float timeStep)
        {
            foreach (var particle in _particles)
            {
                particle.velocity += Vector2.down * timeStep * gravity;
            }

            //viscosity here

            foreach (var particle in _particles)
            {
                particle.previousPosition = particle.Position;
                particle.Position += particle.velocity * timeStep;
            }

            foreach (var particle in _particles) particle.neighbours = NeighboursOf(particle);




            // displacements here

            foreach (var particle in _particles) DoubleDensityRelaxation(particle, timeStep);

            foreach (var particle in _particles) particle.Position += CollisionImpulse(particle, timeStep);

            //ConfineParticlesInBox(_particles, container.Bounds);

            foreach (var particle in _particles)
            {
                particle.velocity = (particle.Position - particle.previousPosition) / timeStep;
            }


        }

        private Vector2 CollisionImpulse(LiquidParticle particle, float timeStep)
        {
            Rect box = container.Bounds;

            if (box.Contains(particle.Position)) return Vector2.zero;

            if (!box.Contains(particle.previousPosition))
            {
                Debug.LogWarning("Particle's previous position was outside the container. This should not happen.");
            }

            (Vector2 collisionPosition, Vector2 collisionNormal) = CollisionToBoxFromInside(box, particle.previousPosition, particle.Position);

            Vector2 velocity = particle.Position - particle.previousPosition;
            
            // components of velocity in the direction of the normal of the box edge and tangential to it
            Vector2 velocityNormal = Vector2.Dot(velocity, collisionNormal) * collisionNormal;
            Vector2 velocityTangent = velocity - velocityNormal;
            
            float tangentialFriction = 0.5f;
           
            
            Vector2 impulse = -velocityNormal - tangentialFriction * velocityTangent;
            
            Vector2 endPosition = particle.Position + impulse;
            if (!box.Contains(endPosition))
            {
               endPosition = ClampToBox(collisionPosition, box);
               impulse = endPosition - particle.Position;
            }



            return impulse;

            Vector2 ClampToBox(Vector2 position, Rect box)
            {
                return new Vector2(Mathf.Clamp(position.x, box.xMin, box.xMax),
                    Mathf.Clamp(position.y, box.yMin, box.yMax));
            }
            

            (Vector2 position, Vector2 normal) CollisionToBoxFromInside(Rect box, Vector2 startPosition, Vector2 attemptedPosition)
            {
                BoxEdge[] boxEdges = 
                {
                    new BoxEdge(new Vector2(box.xMin, box.yMin), new Vector2(box.xMin, box.yMax), Vector2.right),
                    new BoxEdge(new Vector2(box.xMin, box.yMax), new Vector2(box.xMax, box.yMax), Vector2.down),
                    new BoxEdge(new Vector2(box.xMax, box.yMax), new Vector2(box.xMax, box.yMin), Vector2.left),
                    new BoxEdge(new Vector2(box.xMax, box.yMin), new Vector2(box.xMin, box.yMin), Vector2.up)
                };
                
                Vector2 collisionPosition = Vector2.positiveInfinity;
                Vector2 collisionNormal = Vector2.zero;

                foreach (BoxEdge edge in boxEdges)
                {
                    if (LineUtil.IntersectLineSegments2D(startPosition, attemptedPosition, edge.start, edge.end, out Vector2 intersection))
                    {
                        if ((startPosition - intersection).magnitude < (startPosition - collisionPosition).magnitude)
                        {
                            collisionPosition = intersection;
                            collisionNormal = edge.normal;
                        }
                    }
                }

                return (collisionPosition, collisionNormal);
            }
            

            
           
        }

        private void DoubleDensityRelaxation(LiquidParticle particle, float timeStep)
        {
            float density = 0f;
            float nearDensity = 0f;
            
            foreach (var otherParticle in particle.neighbours)
            {
                float distance = (particle.Position - otherParticle.Position).magnitude;
                float q = distance / interactionRadius;
                if (q < 1f)
                {
                    density += Pow2(1f - q);
                    nearDensity += Pow3(1f - q);
                }

            }
    
            float pressure = stiffness * (density - restDensity);
            float nearPressure = nearStiffness * nearDensity;
            
            Vector2 displacement = Vector2.zero;

            foreach (var otherParticle in particle.neighbours)
            {
                float distance = (particle.Position - otherParticle.Position).magnitude;
                float q = distance / interactionRadius;
                if (q < 1f)
                {
                    Vector2 d = Pow2(timeStep) * (pressure * (1f - q) + nearPressure * Pow2(1f - q)) * (otherParticle.Position - particle.Position).normalized;
                    otherParticle.Position += 0.5f * d;
                    displacement -= 0.5f * d;
                }
            }
            particle.Position += displacement;

        }

        /*
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
                float term = _smoothing.Spiky((particle.Position - otherParticle.Position).magnitude)/_smoothing.Spiky(0.2f*interactionRadius);
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
            if (nearbyParticles == null) nearbyParticles = GetLiquidParticlesInCircle(position, interactionRadius);
            
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
        
        
        private List<LiquidParticle> NeighboursOf(LiquidParticle p) => GetLiquidParticlesInCircle(p.Position, interactionRadius);
        

     

         */

        void ConfineParticlesInBox(List<LiquidParticle> particles, Rect boxBounds)
        {
            foreach (var particle in particles)
            {
                if (boxBounds.Contains(particle.Position)) continue;
                Vector2 clampedPosition = new Vector2(Mathf.Clamp(particle.Position.x, boxBounds.xMin, boxBounds.xMax),
                    Mathf.Clamp(particle.Position.y, boxBounds.yMin, boxBounds.yMax));
                particle.Position = clampedPosition;// + Random.insideUnitCircle * 0.1f;

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


        private List<LiquidParticle> NeighboursOf(LiquidParticle p) => GetLiquidParticlesInCircle(p.Position, interactionRadius);
        
        float DensityKernel(float distance) => Pow2(1f - distance / interactionRadius);
        float NearDensityKernel(float distance) => Pow3(1f - distance / interactionRadius);
        float Pow2 (float x) => x * x;
        float Pow3 (float x) => x * x * x;

        #endregion
    }
}
