using System;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation


namespace FluidSimulation
{
    public enum ParticleType
    {
        Liquid, Solid
    }
    
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
        public TextMeshPro text1;
       


     
        private MovingAverage timeAvgCalc;
        private Container container;
        private bool isRunning;
        
        private bool perfTestRunning;
        private Timer perfTestTimer;
        private Timer perfTestTimerUpdate;
        private int perfTestCounter;
        private float perfTestUpdateTime = 0f;
       
        private Visualization _visualization;    
        
        private ParticleData _particleData;
        

        #region ------------------------------------------- UNITY METHODS -----------------------------------------------

        private void Awake()
        {
            container = FindObjectOfType<Container>();
            isRunning = true;
            perfTestRunning = false;
            perfTestTimer = new Timer();
            perfTestTimerUpdate = new Timer();

            _particleData = new ParticleData(10000, 100, interactionRadius);
            _visualization = FindObjectOfType<Visualization>();
            if (_visualization == null) Debug.LogError("No visualization found in the scene.");
        }



        private void Update()
        {
            perfTestTimerUpdate.Reset();
            if (isRunning || Input.GetKeyDown(KeyCode.Period)) Simulate(0.015f);
            
            if (perfTestRunning) perfTestCounter++;
            perfTestUpdateTime += perfTestTimerUpdate.Time;
            
            if (perfTestRunning && perfTestCounter > 60)
            {
                perfTestRunning = false;
                Debug.Log("Perf test took: " + perfTestTimer.Time * 1000f + " ms");
                Debug.Log("Updates took: " + perfTestUpdateTime * 1000f + " ms");

            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("Number of particles: " + _particleData.NumberOfParticles);
               Debug.Log(_particleData._partitioning.DebugInfo());
               // Debug.Log(_particleData.NeighbourhoodWatch());
            }

            if (Input.GetKeyDown(KeyCode.Space)) isRunning = !isRunning;

            if (Input.GetKeyDown(KeyCode.T))
            {
                CreatePerfTestParticles();
                perfTestTimer.Reset();
                perfTestTimerUpdate.Reset();
                
                perfTestCounter = 0;
                perfTestRunning = true;
                perfTestUpdateTime = 0f;
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                CreatePerfTestParticles();
                perfTestTimer.Reset();

                for (int i = 0; i < 60; i++)
                {
                    Simulate(0.015f);
                }
                
                Debug.LogFormat("PERF TEST WITHOUT DRAWING TOOK " + perfTestTimer.Time * 1000f + " ms");
          
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Application.Quit();
            }

            if (Input.GetKeyDown(KeyCode.K))
            {

            }
                
            

         
        }



       

        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public void SpawnParticle(Vector2 position, Vector2 velocity, ParticleType particleType)
        {
            FluidParticle particle = new FluidParticle
            {
                Id=0,
                Position = position,
                PreviousPosition = position,
                Velocity = velocity,
                Type = particleType
            };

            if (_particleData == null)
            {
                Debug.LogError("NO PARTICLES");
                return;
            }
            
            if (_visualization == null)
            {
                Debug.LogError("NO VIS");
                return;
            }

            
            int id = _particleData.Add(particle);
          
            _visualization.AddParticle(id, particleType);
        }
      
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        void RandomNeigh()
        {
            if (_particleData.NumberOfParticles == 0) return;
            int index = Random.Range(0, _particleData.NumberOfParticles);
            ColorParticle(index, Color.red);
            
            foreach(int i in _particleData.NeighbourIndices(index))
            {
                if (i!=index) ColorParticle(i, Color.green);
            }
                
                
       
        }

        void ColorParticle(int index, Color color)
        {
            _visualization.ColorParticle(_particleData.All()[index].Id, color);
        }
        void ResetColors()
        {
            for (int i=0; i<_particleData.NumberOfParticles; i++)
            {
                _visualization.ColorParticle(_particleData.All()[i].Id, Color.blue);
            }
        }
        
        void Simulate(float timeStep)
        {
            
            
            var particles = _particleData.All();
            
            for (int i=0; i<particles.Length; i++)
            {
                particles[i].Velocity += Vector2.down * timeStep * gravity;
            }

            Timer timer = new Timer();
            
            if (viscosityEnabled)
            {
                    ApplyViscosity(timeStep);
            }
            float timeViscosity = timer.Time;
            timer.Reset();

            for (int i=0; i<particles.Length; i++)
            {
                particles[i].PreviousPosition = particles[i].Position;
                particles[i].Position += particles[i].Velocity * timeStep;
            }

            float timeMove = timer.Time;
            timer.Reset();

            _particleData.UpdateNeighbours();
            
            float timeNeigh = timer.Time;
            timer.Reset();
            DoubleDensityRelaxation(timeStep);
            float timeDensity = timer.Time;

            for (int i=0; i<particles.Length; i++)
                particles[i].Position += CollisionImpulse(particles[i], timeStep);

          

            for (int i=0; i<particles.Length; i++)
                particles[i].Velocity = (particles[i].Position - particles[i].PreviousPosition) / timeStep;
           

            for (int i=0; i<particles.Length; i++)
            {
                _visualization.MoveParticle(particles[i].Id, particles[i].Position);
            }
               
            //ResetColors();
           //_particleData.UpdateNeighbours();
         //  ColorCloseCells();
            //RandomNeigh();
//           ColorSpatialGrid();

        //    text1.text = "ParticleData: " + _particleData.Count;
            text1.text = "\nViscosity: " + timeViscosity * 1000f + " ms";
            text1.text += "\nMoving: " + timeMove * 1000f + " ms";
            text1.text += "\nNeigh. search: " + timeNeigh * 1000f + " ms";
            text1.text += "\nDensity: " + timeDensity * 1000f + " ms";

          //  text1.text += "\nAvg. neigh.: " + AvgNumNeighbours();
            //text1.text += "\nMax. neigh.: " + MaxNumNeighbours();
            
        }

   

        private void ColorSpatialGrid()
        {
            var particles = _particleData.All();
            foreach (var particle in particles)
            {
                _visualization.ColorParticle(particle.Id, Color.blue);
            }
            
            foreach (var cell in _particleData._partitioning._cells)
            {
                Color rndColor = _visualization.RandomColor;
                foreach ((int index, Vector2 pos) in cell.Value)
                {
                    int id = _particleData.All()[index].Id;
                    _visualization.ColorParticle(id, rndColor);
                }
            }
        }

        private void ApplyViscosity( float timeStep)
        {
            var particles = _particleData.All();
            foreach ( (int a, int b) in _particleData.NeighbourParticlePairs())
            {
                float q = (particles[a].Position - particles[b].Position).magnitude / interactionRadius;
                if (q>=1f) continue;
                Vector2 r = (particles[a].Position - particles[b].Position).normalized;
                // Inward radial velocity
                float u = Vector2.Dot(particles[a].Velocity - particles[b].Velocity,  r);
                
                if (u <= 0f) continue;
                
                Vector2 impulse = timeStep * (1f - q) * (sigma * u + beta * Pow2(u)) * r;
                particles[a].Velocity -= impulse * 0.5f;
                particles[b].Velocity += impulse * 0.5f;

            }
            
          
        }


        private Vector2 CollisionImpulse(FluidParticle particle, float timeStep)
        {
            Rect box = container.Bounds;

            if (box.Contains(particle.Position)) return Vector2.zero;

            if (!box.Contains(particle.PreviousPosition))
            {
                Debug.LogWarning("Particle's previous position was outside the container. This should not happen.");
            }

            (Vector2 collisionPosition, Vector2 collisionNormal) = CollisionToBoxFromInside(box, particle.PreviousPosition, particle.Position);

            Vector2 velocity = particle.Position - particle.PreviousPosition;
            
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

        private void DoubleDensityRelaxation(float timeStep)
        {
            var particles = _particleData.All();
            
            

            for (int i=0; i<particles.Length; i++)
            {
                float density = 0f;
                float nearDensity = 0f;
                
                var neighbours = _particleData.NeighbourIndices(i);
                
                foreach (int j in neighbours)
                {
                    if (i == j) continue;
                   
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
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

                foreach (int j in neighbours)
                {
                    if (i == j) continue;
               
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
                    float q = distance / interactionRadius;
                    if (q < 1f)
                    {
                        Vector2 d = Pow2(timeStep) * (pressure * (1f - q) + nearPressure * Pow2(1f - q)) *
                                    (particles[j].Position - particles[i].Position).normalized;
                        particles[j].Position += 0.5f * d;
                        displacement -= 0.5f * d;
                    }
                }

                particles[i].Position += displacement;
            }

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
          
                SpawnParticle(position, Vector2.zero, ParticleType.Liquid);
            }
        }


    
        

        
       
        float Pow2 (float x) => x * x;
        float Pow3 (float x) => x * x * x;
/*
        
        float AvgNumNeighbours()
        {
            if (_particleData.All().Length == 0) return 0f;
            float sum = 0f;
            foreach (var particle in _particleData)
            {
                sum += particle.neighbours.Count();
            }

            return sum / _particleData.All().Length;
        }
        
        int MaxNumNeighbours()
        {
            int max = 0;
            foreach (var particle in _particleData)
            {
                if (particle.neighbours.Count()> max) max = particle.neighbours.Count();
            }

            return max;
        }*/
        #endregion
      
    }
}
