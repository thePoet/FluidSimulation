using System;
using UnityEngine;

// Based on paper by Simon Clavet, Philippe Beaudoin, and Pierre Poulin
// https://www.academia.edu/452554/Particle-Based_Viscoelastic_Fluid_Simulation

namespace FluidSimulation
{
    
    public class ParticleDynamics 
    {
        public struct Settings
        {
            public float InteractionRadius;
            public float Gravity;
            public float RestDensity;
            public float Stiffness;
            public float NearStiffness;
            public float ViscositySigma;
            public float ViscosityBeta;
        }
        
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
        private readonly Settings _settings;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleDynamics(Settings settings, Rect bounds)
        {
            _settings = settings;
            _bounds = bounds;
        }
        
        public void Step(ParticleData particleData, float timeStep)
        {
            var particles = particleData.All();
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].Velocity += Vector2.down * timeStep * _settings.Gravity;
            }
            
            if (IsViscosityEnabled())
            {
                ApplyViscosity(particles, particleData.NeighbourParticlePairs(), timeStep);
            }
            
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].PreviousPosition = particles[i].Position;
                particles[i].Position += particles[i].Velocity * timeStep;
            }

            
            particleData.UpdateNeighbours();
     
            MaintainDensity(particleData, timeStep);
 
            for (int i=0; i<particles.Length; i++)
                particles[i].Position += CollisionImpulse(particles[i]);
            

            for (int i=0; i<particles.Length; i++)
                particles[i].Velocity = (particles[i].Position - particles[i].PreviousPosition) / timeStep;
           

            bool IsViscosityEnabled() => _settings.ViscositySigma > 0f || _settings.ViscosityBeta > 0f;
        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        /// <summary>
        /// Maintains the density of the fluid by moving particles with Double Density Relaxation method.
        /// See section 4. in the Beaudoin et al. paper.
        /// </summary>
        private void MaintainDensity(ParticleData particleData,  float timeStep)
        {

            var particles = particleData.All();
            
            
            for (int i=0; i<particles.Length; i++)
            {
                float density = 0f;
                float nearDensity = 0f;
                
                var neighbours = particleData.NeighbourIndices(i);
                
                foreach (int j in neighbours)
                {
                    if (i == j) continue;
                   
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
                    if (i == j) continue;
               
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

                if (particles[i].Type == ParticleType.Liquid)
                    particles[i].Position += displacement;
            }

        }

        private void ApplyViscosity(Span<FluidParticle> particles, Span<(int,int)> particlePairs, float timeStep)
        {
            var interactionRadius = _settings.InteractionRadius;
            var sigma = _settings.ViscositySigma;
            var beta = _settings.ViscosityBeta;
            
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


        private Vector2 CollisionImpulse(FluidParticle particle)
        {
            if (_bounds.Contains(particle.Position)) return Vector2.zero;

            if (!_bounds.Contains(particle.PreviousPosition))
            {
                Debug.LogWarning("Particle's previous position was outside the container. This should not happen.");
            }

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
                endPosition = ClampToBox(collisionPosition, Shrink(_bounds, _bounds.width * 0.0001f));
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
