using System;
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
    
    public class Simulation 
    {
        public struct Settings
        {
            public float interactionRadius;
            public float gravity;
            public float restDensity;
            public float stiffness;
            public float nearStiffness;
            public float viscositySigma;
            public float viscosityBeta;
        }
        
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

        public TextMeshPro text1;
     
        private MovingAverage timeAvgCalc;
        private Container _container;
        private bool isRunning;
        
        private bool perfTestRunning;
        private Timer perfTestTimer;
        private Timer perfTestTimerUpdate;
        private int perfTestCounter;
        private float perfTestUpdateTime = 0f;
       
     
        
        private ParticleData _particleData;
        private Settings _settings;


        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public Simulation(Settings defaultSettings, Container container)
        {
            _settings = defaultSettings;
            _container = container;
            _particleData = new ParticleData(10000, 100, _settings.interactionRadius);
        
        }
        
       

  
        public int SpawnParticle(Vector2 position, Vector2 velocity, ParticleType particleType)
        {
            FluidParticle particle = new FluidParticle
            {
                Id = 0,
                Position = position,
                Velocity = velocity,
                Type = particleType
            };

            
            int id = _particleData.Add(particle);

            return id;
        }
        
        public Span<FluidParticle> AllParticles()
        {
            return _particleData.All();
        }
      
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
/*
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
        */
        /*

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
        }*/
        
        public void Step(float timeStep)
        {
            var particles = _particleData.All();
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].Velocity += Vector2.down * timeStep * _settings.gravity;
            }
            
            if (IsViscosityEnabled())
            {
                ApplyViscosity(particles, _particleData.NeighbourParticlePairs(), timeStep);
            }
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].PreviousPosition = particles[i].Position;
                particles[i].Position += particles[i].Velocity * timeStep;
            }

            
            _particleData.UpdateNeighbours();
            
            MaintainDensity(_particleData.All(), timeStep);
 

            for (int i=0; i<particles.Length; i++)
                particles[i].Position += CollisionImpulse(particles[i], timeStep);
            

            for (int i=0; i<particles.Length; i++)
                particles[i].Velocity = (particles[i].Position - particles[i].PreviousPosition) / timeStep;
           
            
/*   
            text1.text = "\nViscosity: " + timeViscosity * 1000f + " ms";
            text1.text += "\nMoving: " + timeMove * 1000f + " ms";
            text1.text += "\nNeigh. search: " + timeNeigh * 1000f + " ms";
            text1.text += "\nDensity: " + timeDensity * 1000f + " ms";
*/

            bool IsViscosityEnabled() => _settings.viscositySigma > 0f || _settings.viscosityBeta > 0f;
        }

        /// <summary>
        /// Maintains the density of the fluid by moving particles with Double Density Relaxation method.
        /// See section 4. in the Beaudoin et al. paper.
        /// </summary>
        private void MaintainDensity(Span<FluidParticle> particles, float timeStep)
        {
            for (int i=0; i<particles.Length; i++)
            {
                float density = 0f;
                float nearDensity = 0f;
                
                var neighbours = _particleData.NeighbourIndices(i);
                
                foreach (int j in neighbours)
                {
                    if (i == j) continue;
                   
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
                    float q = distance / _settings.interactionRadius;
                    if (q < 1f)
                    {
                        density += Pow2(1f - q);
                        nearDensity += Pow3(1f - q);
                    }
                }

                float pressure = _settings.stiffness * (density - _settings.restDensity);
                float nearPressure = _settings.nearStiffness * nearDensity;
                Vector2 displacement = Vector2.zero;

                foreach (int j in neighbours)
                {
                    if (i == j) continue;
               
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
                    float q = distance / _settings.interactionRadius;
                    if (q < 1f)
                    {
                        Vector2 d = Pow2(timeStep) * (pressure * (1f - q) + nearPressure * Pow2(1f - q)) *
                                    (particles[j].Position - particles[i].Position).normalized;
                        if (particles[j].Type == ParticleType.Liquid)
                            particles[j].Position += 0.5f * d;
                        displacement -= 0.5f * d;
                    }
                }

                if (particles[i].Type == ParticleType.Liquid)
                    particles[i].Position += displacement;
            }

        }
/*
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
        }*/

        private void ApplyViscosity(Span<FluidParticle> particles, Span<(int,int)> particlePairs, float timeStep)
        {
            var interactionRadius = _settings.interactionRadius;
            var sigma = _settings.viscositySigma;
            var beta = _settings.viscosityBeta;
            
            foreach ( (int a, int b) in particlePairs)
            {
                if (particles[a].Type == ParticleType.Solid || particles[b].Type == ParticleType.Solid) continue;
                
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
            Rect box = _container.Bounds;

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

        void CreatePerfTestParticles()
        {
            Random.InitState(123);
         
            for (int i = 0; i < 500; i++)
            {
                Vector2 position = new Vector2
                (
                    x: Random.Range( _container.Bounds.xMin, _container.Bounds.xMax),
                    y: Random.Range( _container.Bounds.yMin, _container.Bounds.yMax)
                );
          
                SpawnParticle(position, Vector2.zero, ParticleType.Liquid);
            }
        }
        
       
        float Pow2 (float x) => x * x;
        float Pow3 (float x) => x * x * x;

        #endregion
      
    }
}
