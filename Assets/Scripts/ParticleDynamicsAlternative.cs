using UnityEngine;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation

namespace FluidSimulation
{
    
    public class ParticleDynamicsAlternative : IParticleDynamics
    {
        [System.Serializable]

        
        private struct BoxEdge
        {
            public BoxEdge(Vector2 start, Vector2 end, Vector2 normal)
            {
                this.Start = start;
                this.End = end;
                this.Normal = normal;
            }
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public readonly Vector2 Normal;
        }
     
        private Rect _bounds;
        private readonly ParticleDynamics.Settings _settings;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleDynamicsAlternative(ParticleDynamics.Settings settings, Rect bounds)
        {
            _settings = settings;
            _bounds = bounds;
        }
        
     
        
        public void Step(IParticleData particleData, float timeStep)
        {
            var particles = particleData.All();
            
            // External forces (gravity)
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].Velocity += Vector2.down * timeStep * _settings.Gravity;
            }
            
            if (_settings.ViscosityEnabled)
            {
                ApplyViscosity(particleData, timeStep);
            }
            
            // Move particles due to their velocity
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].PreviousPosition = particles[i].Position;
                particles[i].Position += particles[i].Velocity * timeStep;
            }

            
            particleData.UpdateNeighbours();
      
            MaintainDensity(particleData, timeStep);
 
            
            
            for (int i=0; i<particles.Length; i++)
                particles[i].Position += CollisionImpulseFromBorders(particles[i]);
            

            for (int i=0; i<particles.Length; i++)
                particles[i].Velocity = (particles[i].Position - particles[i].PreviousPosition) / timeStep;
           

        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        /// <summary>
        /// Maintains the density of the fluid by moving particles with Double Density Relaxation method.
        /// See section 4. in the Beaudoin et al. paper.
        /// </summary>
        private void MaintainDensity(IParticleData particleData,  float timeStep)
        {
             var particles = particleData.All();
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                
                float density = 0f;
                float nearDensity = 0f;
                
                var neighbours = particleData.NeighbourIndices(i);
                
                foreach (int j in neighbours)
                {
                    float distance = (particles[i].Position - particles[j].Position).magnitude;
                    float q = distance / _settings.InteractionRadius;
                    if (q < 1f)
                    {
                        density += Pow2(1f - q);
                        nearDensity += Pow3(1f - q);
                    }
                }

                float pressure = _settings.Stiffness * (density - _settings.RestDensity);
                float nearPressure = _settings.NearStiffness * nearDensity;
                Vector2 displacement = Vector2.zero;

                foreach (int j in neighbours)
                {
                    if (particles[j].Type == ParticleType.Solid) continue;

                    float distance = (particles[i].Position - particles[j].Position).magnitude;
                    float q = distance / _settings.InteractionRadius;
                    if (q < 1f)
                    {
                        Vector2 d = Pow2(timeStep) * (pressure * (1f - q) + nearPressure * Pow2(1f - q)) *
                                    (particles[j].Position - particles[i].Position).normalized;
                        
                        
                        if (particles[j].Type == ParticleType.Liquid)
                            particles[j].Position += 0.5f * d;
                        displacement -= 0.5f * d;
                    }
                }
      
                //particles[i].Position += displacement;
                particles[i].PosChange = displacement;

     
            }
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].Position += particles[i].PosChange;
            }
        
            
          
         
        }
        


        
        private void ApplyViscosity(IParticleData particleData,  float timeStep)
        {
            var particles = particleData.All();
            
            var interactionRadius = _settings.InteractionRadius;
            var sigma = _settings.ViscositySigma;
            var beta = _settings.ViscosityBeta;
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                
                foreach (int j in particleData.NeighbourIndices(i))
                {
                    if (i==j) continue;
                    
                    if (particles[j].Type == ParticleType.Solid) continue;
                
                    float q = (particles[i].Position - particles[j].Position).magnitude / interactionRadius;
                    if (q>=1f) continue;
                    Vector2 r = (particles[i].Position - particles[j].Position).normalized;
                    // Inward radial velocity
                    float u = Vector2.Dot(particles[i].Velocity - particles[j].Velocity,  r);
                
                    if (u <= 0f) continue;
                
                    Vector2 impulse = timeStep * (1f - q) * (sigma * u + beta * Pow2(u)) * r;
                    particles[i].Velocity -= impulse * 0.5f;
                    //particles[j].Velocity += impulse * 0.5f;
                }
   
            }
            
            
          
        }


        private Vector2 CollisionImpulseFromBorders(FluidParticle particle)
        {
            if (_bounds.Contains(particle.Position)) return Vector2.zero;

            (Vector2 collisionPosition, Vector2 collisionNormal) = CollisionToBoxFromInside(_bounds, particle.PreviousPosition, particle.Position);

            Vector2 velocity = particle.Position - particle.PreviousPosition;
            
            // components of velocity in the direction of the normal of the box edge and tangential to it
            Vector2 velocityNormal = Vector2.Dot(velocity, collisionNormal) * collisionNormal;
            Vector2 velocityTangent = velocity - velocityNormal;
            
            float tangentialFriction = 0.5f;
            
            Vector2 impulse = -velocityNormal - tangentialFriction * velocityTangent;
            
            Vector2 endPosition = particle.Position + impulse;
            if (!_bounds.Contains(endPosition))
            {
                endPosition = ClampToBox(endPosition, Shrink(_bounds, _bounds.width * 0.0001f));
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
                
                Vector2 collPosition = Vector2.positiveInfinity;
                Vector2 collNormal = Vector2.zero;

                foreach (BoxEdge edge in boxEdges)
                {
                    if (LineUtil.IntersectLineSegments2D(startPosition, attemptedPosition, edge.Start, edge.End, out Vector2 intersection))
                    {
                        if ((startPosition - intersection).magnitude < (startPosition - collPosition).magnitude)
                        {
                            collPosition = intersection;
                            collNormal = edge.Normal;
                        }
                    }
                }

                return (collPosition, collNormal);
            }
        }

    
        float Pow2 (float x) => x * x;
        float Pow3 (float x) => x * x * x;



     

        #endregion
      
    }
}
