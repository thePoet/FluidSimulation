using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation


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

        public float interactionRadius;
        public float gravity;
        
        [Header("Density & incompressibility")]
        public float restDensity;
        public float stiffness;
        public float nearStiffness;
        
        
        [Header("Viscosity")]
        public bool viscosityEnabled = true;
        public float sigma = 0.0f;
        public float beta = 0.3f;

        [Header(" ")]
        public GameObject liquidParticlePrefab;
        public TextMeshPro text1;
        public TextMeshPro text2;
       


        private static List<LiquidParticle> _particles;
        private MovingAverage timeAvgCalc;
        private Texture2D densityTexture;
        private Container container;
        private bool isRunning;
        
        private bool perfTestRunning;
        private Timer perfTestTimer;
        private int perfTestCounter;

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

            perfTestRunning = false;
            perfTestTimer = new Timer();

        }



        private void Update()
        {
            if (isRunning || Input.GetKeyDown(KeyCode.Period)) Simulate(0.015f);

            if (perfTestRunning) perfTestCounter++;
            if (perfTestRunning && perfTestCounter > 60)
            {
                perfTestRunning = false;
                Debug.Log("Perf test took: " + perfTestTimer.Time * 1000f + " ms");
            }

            if (Input.GetKeyDown(KeyCode.N)) Debug.Log("Number of particles: " + _particles.Count);

            if (Input.GetKeyDown(KeyCode.Space)) isRunning = !isRunning;

            if (Input.GetKeyDown(KeyCode.T))
            {
                CreatePerfTestParticles();
                perfTestTimer.Reset();
                perfTestCounter = 0;
                perfTestRunning = true;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                foreach (var particle in _particles)
                {
                    Destroy(particle.gameObject);
                }
                _particles.Clear();
                LiquidParticle.partitioning.RemoveAllEntities();
            }

        }



        void OnDrawGizmos()
        {

            //DrawDensityMap();
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

        public void SpawnParticleAt(Vector2 position, Vector2 velocity)
        {
            GameObject particle = Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            particle.GetComponent<LiquidParticle>().velocity = velocity;
        }
        
        public void SpawnParticle2At(Vector2 position, Vector2 velocity)
        {
            GameObject particle = Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            particle.GetComponent<LiquidParticle>().velocity = velocity;
            particle.GetComponent<LiquidParticle>().Color = Color.blue;
            particle.GetComponent<LiquidParticle>().gravityMultiplier = -0.5f;
            particle.GetComponent<LiquidParticle>().movementMultiplier = 0f;
            
        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        void Simulate(float timeStep)
        {
            foreach (var particle in _particles)
            {
                particle.velocity += Vector2.down * timeStep * gravity * particle.gravityMultiplier;
            }

            if (viscosityEnabled)
            {
                foreach (var (particleA, particleB) in AllNeighbouringParticlePairs())
                {
                    ApplyViscosity(particleA, particleB, timeStep);
                }
            }

            foreach (var particle in _particles)
            {
                particle.previousPosition = particle.Position;
                particle.Position += particle.velocity * timeStep * particle.movementMultiplier;
            }

            Timer timer = new Timer();
           // foreach (var particle in _particles) particle.neighbours = NeighboursOf(particle);
            foreach (var particle in _particles) particle.UpdateNeighbours();
            float time = timer.Time;
            
            foreach (var particle in _particles) DoubleDensityRelaxation(particle, timeStep);

            foreach (var particle in _particles) particle.Position += CollisionImpulse(particle, timeStep) * particle.movementMultiplier;

          

            foreach (var particle in _particles)
            {
                particle.velocity = (particle.Position - particle.previousPosition) / timeStep;
            }

            text1.text = "Particles: " + _particles.Count;
            text2.text = "Neigh. search: " + time * 1000f + " ms";
            text2.text += "\nAvg. neigh.: " + AvgNumNeighbours();
            text2.text += "\nMax. neigh.: " + MaxNumNeighbours();
            
        }

        private void ApplyViscosity(LiquidParticle particleA, LiquidParticle particleB, float timeStep)
        {
            float q = (particleA.Position - particleB.Position).magnitude / interactionRadius;
            if (q < 1f)
            {
                Vector2 r = (particleA.Position - particleB.Position).normalized;
                // Inward radial velocity
                float u = Vector2.Dot(particleA.velocity - particleB.velocity,  r);
                
                if (u <= 0f) return;
                
                Vector2 impulse = timeStep * (1f - q) * (sigma * u + beta * Pow2(u)) * r;
                particleA.velocity -= impulse * 0.5f;
                particleB.velocity += impulse * 0.5f;
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
                endPosition = ClampToBox(collisionPosition, Shrink(box, box.width * 0.0001f));
               impulse = endPosition - particle.Position;
            }



            return impulse;

            Vector2 ClampToBox(Vector2 position, Rect box)
            {
                return new Vector2(Mathf.Clamp(position.x, box.xMin, box.xMax),
                    Mathf.Clamp(position.y, box.yMin, box.yMax));
            }
            
            Rect Shrink(Rect rect, float amount)
            {
                return new Rect(rect.xMin + amount/2f, rect.yMin + amount/2f, rect.width - amount, rect.height - amount);
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
                if (otherParticle == particle) continue;
                
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
                if (otherParticle == particle) continue;

                float distance = (particle.Position - otherParticle.Position).magnitude;
                float q = distance / interactionRadius;
                if (q < 1f)
                {
                    Vector2 d = Pow2(timeStep) * (pressure * (1f - q) + nearPressure * Pow2(1f - q)) * (otherParticle.Position - particle.Position).normalized;
                    otherParticle.Position += 0.5f * d * otherParticle.movementMultiplier;
                    displacement -= 0.5f * d;
                }
            }
            particle.Position += displacement * particle.movementMultiplier;

        }
        

     

  

        void CreatePerfTestParticles()
        {
            Random.InitState(123);
         
            for (int i = 0; i < 500; i++)
            {
                Vector2 position = new Vector2
                (
                    x: Random.Range( container.Bounds.xMin, container.Bounds.xMax),
                    y: Random.Range( container.Bounds.yMin, container.Bounds.yMax)
                );
          
                SpawnParticleAt(position, Vector2.zero);
            }
        }


        private List<LiquidParticle> NeighboursOf(LiquidParticle p) => GetLiquidParticlesInCircle(p.Position, interactionRadius);
        
        private IEnumerable<(LiquidParticle, LiquidParticle)> AllNeighbouringParticlePairs()
        {
            foreach (LiquidParticle particleA in _particles)
            {
                foreach (LiquidParticle particleB in particleA.neighbours)
                {
                    if (particleA.GetInstanceID() < particleB.GetInstanceID()) yield return (particleA, particleB);
                }
            }
        }
        
       
        float Pow2 (float x) => x * x;
        float Pow3 (float x) => x * x * x;

        
        float AvgNumNeighbours()
        {
            if (_particles.Count == 0) return 0f;
            float sum = 0f;
            foreach (var particle in _particles)
            {
                sum += particle.neighbours.Count;
            }

            return sum / _particles.Count;
        }
        
        int MaxNumNeighbours()
        {
            int max = 0;
            foreach (var particle in _particles)
            {
                if (particle.neighbours.Count> max) max = particle.neighbours.Count;
            }

            return max;
        }
        #endregion
    }
}
