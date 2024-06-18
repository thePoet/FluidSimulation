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
                Start = start;
                End = end;
                Normal = normal;
            }
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public readonly Vector2 Normal;
        }
        private ComputeShader _dynamicsComputeShader;
        
        
        
        private Rect _bounds;
        private readonly ParticleDynamics.Settings _settings;
        private ComputeBuffer _particleBuffer;
        private ComputeBuffer _cellParticleCount;
        private ComputeBuffer _particlesInCells;
        private ComputeBuffer _particleNeighbours;
        private ComputeBuffer _particleNeighbourCount;
        private ComputeBuffer _changeBuffer;
      
        // Kernel indices
        private const int ClearPartitioningKernel            = 0;
        private const int FillPartitioningKernel             = 1;
        private const int FindNeighboursKernel               = 2;
        private const int CalculateViscosityKernel           = 3;
        private const int ApplyViscosityKernel               = 4;
        private const int ApplyVelocityKernel                = 5;
        private const int CalculateDensityDisplacementKernel = 6;
        private const int ApplyDensityDisplacementKernel     = 7;

        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleDynamicsAlternative(ParticleDynamics.Settings settings, Rect bounds)
        {
            _settings = settings;
            _bounds = bounds;
        }
        
       
        
        public void TemporaryInit(ComputeShader computeShader, IParticleData pdata)
        {
            _dynamicsComputeShader = computeShader;
            _particleBuffer = pdata.CreateParticlesBuffer();
            
            _dynamicsComputeShader.SetBuffer(1, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(2, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(3, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(4, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(5, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(6, "_Particles", _particleBuffer);
            _dynamicsComputeShader.SetBuffer(7, "_Particles", _particleBuffer);

            _dynamicsComputeShader.SetInt("_MaxNumParticles", pdata.MaxNumberOfParticles);
           _dynamicsComputeShader.SetFloat("_AreaMinX", _bounds.xMin);
           _dynamicsComputeShader.SetFloat("_AreaMinY", _bounds.yMin);
           _dynamicsComputeShader.SetFloat("_AreaMaxX", _bounds.xMax);
           _dynamicsComputeShader.SetFloat("_AreaMaxY", _bounds.yMax);

           int maxNumParticlesInCell = 25;
           _dynamicsComputeShader.SetInt("_MaxNumParticlesPerCell", maxNumParticlesInCell);
           _dynamicsComputeShader.SetFloat("_InteractionRadius", _settings.InteractionRadius);
           
           int numberOfCells = Mathf.CeilToInt(_bounds.width / _settings.InteractionRadius) 
                               * Mathf.CeilToInt(_bounds.height / _settings.InteractionRadius);

           _cellParticleCount = new ComputeBuffer(numberOfCells, sizeof(int));
           _particlesInCells = new ComputeBuffer(maxNumParticlesInCell*numberOfCells, sizeof(int));
           
           _dynamicsComputeShader.SetBuffer(0, "_CellParticleCount", _cellParticleCount);
           _dynamicsComputeShader.SetBuffer(1, "_CellParticleCount", _cellParticleCount);
           _dynamicsComputeShader.SetBuffer(2, "_CellParticleCount", _cellParticleCount);
           _dynamicsComputeShader.SetBuffer(1, "_ParticlesInCells", _particlesInCells);
           _dynamicsComputeShader.SetBuffer(2, "_ParticlesInCells", _particlesInCells);
           
           int maxNumNeighbours = 100;
           _dynamicsComputeShader.SetInt("_MaxNumNeighbours", maxNumNeighbours);
           _particleNeighbours = new ComputeBuffer(pdata.MaxNumberOfParticles * maxNumNeighbours , sizeof(int));
           _particleNeighbourCount = new ComputeBuffer(pdata.MaxNumberOfParticles , sizeof(int));

           _dynamicsComputeShader.SetBuffer(2, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(2, "_ParticleNeighbourCount", _particleNeighbourCount);
           _dynamicsComputeShader.SetBuffer(3, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(3, "_ParticleNeighbourCount", _particleNeighbourCount);
           _dynamicsComputeShader.SetBuffer(4, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(4, "_ParticleNeighbourCount", _particleNeighbourCount);
           _dynamicsComputeShader.SetBuffer(6, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(6, "_ParticleNeighbourCount", _particleNeighbourCount);
           _dynamicsComputeShader.SetBuffer(7, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(7, "_ParticleNeighbourCount", _particleNeighbourCount);

           
           _changeBuffer = new ComputeBuffer(pdata.MaxNumberOfParticles * maxNumNeighbours , 2*sizeof(float));
           _dynamicsComputeShader.SetBuffer(3, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(4, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(6, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(7, "_ChangeBuffer", _changeBuffer);
        }
        
        public void TemporaryRelease()
        {
            _particleBuffer.Release();
            _cellParticleCount.Release();
            _particlesInCells.Release();
            _particleNeighbours.Release();
            _particleNeighbourCount.Release();
            _changeBuffer.Release();
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
            
           // float t1= Time.realtimeSinceStartup;
           particleData.WriteParticlesToBuffer(_particleBuffer);
            _dynamicsComputeShader.SetInt("_NumParticles", particleData.NumberOfParticles);
            _dynamicsComputeShader.SetFloat("_Time", timeStep);
 
                // tsekkaa määrät
            _dynamicsComputeShader.Dispatch(ClearPartitioningKernel,  32, 16, 1);
            _dynamicsComputeShader.Dispatch(FillPartitioningKernel,   32, 16, 1);
            _dynamicsComputeShader.Dispatch(FindNeighboursKernel,     32, 16, 1);
            _dynamicsComputeShader.Dispatch(CalculateViscosityKernel, 32, 16, 1);
            _dynamicsComputeShader.Dispatch(ApplyViscosityKernel,     32, 16, 1);

            _dynamicsComputeShader.Dispatch(ApplyVelocityKernel,     32, 16, 1);
            
            particleData.ReadParticlesFromBuffer(_particleBuffer);
            particleData.ReadNeighboursFromBuffer(_particleNeighbours, _particleNeighbourCount);

         
            
            /*
            // Move particles due to their velocity
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].PreviousPosition = particles[i].Position;
                particles[i].Position += particles[i].Velocity * timeStep;
            }*/

            
         //   particleData.UpdateNeighbours();
      
            MaintainDensity(particleData, timeStep);
 
            
            particles = particleData.All();
            
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
                
                var neighbours = particleData.NeighbourIndicesTest(i);
                
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
      
                particles[i].Position += displacement;
            //    particles[i].Change = displacement;

     
            }
            /*
            for (int i=0; i<particles.Length; i++)
            {
                if (particles[i].Type == ParticleType.Solid) continue;
                particles[i].Position += particles[i].Change;
            }
        */
            
          
         
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
