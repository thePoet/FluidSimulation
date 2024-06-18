using RikusGameDevToolbox.GeneralUse;
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
        private ComputeBuffer _statsBuffer;
      
        // Kernel indices TODO Use findKernel
        private const int ClearPartitioningKernel            = 0;
        private const int FillPartitioningKernel             = 1;
        private const int FindNeighboursKernel               = 2;
        private const int CalculateViscosityKernel           = 3;
        private const int ApplyViscosityKernel               = 4;
        private const int ApplyVelocityKernel                = 5;
        private const int CalculatePressuresKernel           = 6;
        private const int CalculateDensityDisplacementKernel = 7;
        private const int ApplyDensityDisplacementKernel     = 8;

        
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
            _dynamicsComputeShader.SetBuffer(8, "_Particles", _particleBuffer);
            
            
            _statsBuffer = new ComputeBuffer(10 , sizeof(int));
            _dynamicsComputeShader.SetBuffer(0, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(1, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(2, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(3, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(4, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(5, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(6, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(7, "_Stats", _statsBuffer);
            _dynamicsComputeShader.SetBuffer(8, "_Stats", _statsBuffer);

            

            _dynamicsComputeShader.SetInt("_MaxNumParticles", pdata.MaxNumberOfParticles);
           _dynamicsComputeShader.SetFloat("_AreaMinX", _bounds.xMin-1f);
           _dynamicsComputeShader.SetFloat("_AreaMinY", _bounds.yMin-1f);
           _dynamicsComputeShader.SetFloat("_AreaMaxX", _bounds.xMax+1f);
           _dynamicsComputeShader.SetFloat("_AreaMaxY", _bounds.yMax+1f);

           int maxNumParticlesInCell = 50;
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
           _dynamicsComputeShader.SetBuffer(8, "_ParticleNeighbours", _particleNeighbours);
           _dynamicsComputeShader.SetBuffer(8, "_ParticleNeighbourCount", _particleNeighbourCount);

           
           _changeBuffer = new ComputeBuffer(pdata.MaxNumberOfParticles * maxNumNeighbours , 2*sizeof(float));
           _dynamicsComputeShader.SetBuffer(3, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(4, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(7, "_ChangeBuffer", _changeBuffer);
           _dynamicsComputeShader.SetBuffer(8, "_ChangeBuffer", _changeBuffer);
        }
        
        public void TemporaryRelease()
        {
            _particleBuffer.Release();
            _cellParticleCount.Release();
            _particlesInCells.Release();
            _particleNeighbours.Release();
            _particleNeighbourCount.Release();
            _changeBuffer.Release();
            _statsBuffer.Release();
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
 
            
               
            _dynamicsComputeShader.Dispatch(ClearPartitioningKernel,  32, 16, 1);
            _dynamicsComputeShader.Dispatch(FillPartitioningKernel,   32, 16, 1);
            _dynamicsComputeShader.Dispatch(FindNeighboursKernel,     32, 16, 1);
            
            _dynamicsComputeShader.Dispatch(CalculateViscosityKernel, 32, 16, 1);
            _dynamicsComputeShader.Dispatch(ApplyViscosityKernel,     32, 16, 1);

            _dynamicsComputeShader.Dispatch(ApplyVelocityKernel,     32, 16, 1);
            
            _dynamicsComputeShader.Dispatch(ClearPartitioningKernel,  32, 16, 1);
            _dynamicsComputeShader.Dispatch(FillPartitioningKernel,   32, 16, 1);
            _dynamicsComputeShader.Dispatch(FindNeighboursKernel,     32, 16, 1);
            
            _dynamicsComputeShader.Dispatch(CalculatePressuresKernel,     32, 16, 1);
            _dynamicsComputeShader.Dispatch(CalculateDensityDisplacementKernel,     32, 16, 1);
            _dynamicsComputeShader.Dispatch(ApplyDensityDisplacementKernel,     32, 16, 1);
            
            
            particleData.ReadParticlesFromBuffer(_particleBuffer);
            particleData.ReadNeighboursFromBuffer(_particleNeighbours, _particleNeighbourCount);
        
            particles = particleData.All();

            for (int i=0; i<particles.Length; i++)
               particles[i].Position += CollisionImpulseFromBorders(particles[i]);
            
   

            for (int i=0; i<particles.Length; i++)
                particles[i].Velocity = (particles[i].Position - particles[i].PreviousPosition) / timeStep;
   
           // for (int i=0; i<particles.Length; i++)
             //   particles[i] = KeepInBox(particles[i]);

            PrintStats(_statsBuffer);

        }

        private void PrintStats(ComputeBuffer statsBuffer)
        {
            int[] stats = new int[10];
            statsBuffer.GetData(stats);
            Debug.Log("Stats: " + stats[0] + " " + stats[1] + " " + stats[2] + " " + stats[3] + " " + stats[4] + " " + stats[5] + " " + stats[6] + " " + stats[7] + " " + stats[8] + " " + stats[9]);
        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------



        private FluidParticle KeepInBox(FluidParticle particle)
        {
            if (_bounds.Contains(particle.Position)) return particle;
            
            particle.Position = ClampToBox(particle.Position, _bounds);

            return particle;
            
            /*
            float damping = -0.5f;

            float e = Random.Range(0.001f, 0.1f);
            
            if (particle.Position.x < _bounds.xMin)
            {
                particle.Velocity = particle.Velocity.SetX(particle.Velocity.x * damping);
                particle.Position = particle.Position.SetX(_bounds.xMin+e);
            }
            else if (particle.Position.x > _bounds.xMax)
            {
                particle.Velocity = particle.Velocity.SetX(particle.Velocity.x * damping);
                particle.Position = particle.Position.SetX(_bounds.xMax-e);
            }
            
            if (particle.Position.y < _bounds.yMin)
            {
                particle.Velocity = particle.Velocity.SetY(particle.Velocity.y * damping);
                particle.Position = particle.Position.SetY(_bounds.yMin+e);
            }
            else if (particle.Position.y > _bounds.yMax)
            {
                particle.Velocity = particle.Velocity.SetY(particle.Velocity.y * damping);
                particle.Position = particle.Position.SetY(_bounds.yMax-e);
            }
            
            //particle.Position = MoveTowardsBoxCenter(particle.Position, Random.Range(0.01f,0.02f), _bounds);
            //particle.Position += Random.insideUnitCircle * 0.001f;
            return particle;
            /*
        }

        particle.Velocity = -particle.Velocity * 0.5f;

        particle.Position =  ClampToBox(particle.Position, _bounds);
        particle.Position = MoveTowardsBoxCenter(particle.Position, 0.2f, _bounds);
        particle.Position += Random.insideUnitCircle * 0.1f;
        */
           
            
            Vector2 ClampToBox(Vector2 position, Rect box)
            {
                return new Vector2(Mathf.Clamp(position.x, box.xMin, box.xMax),
                    Mathf.Clamp(position.y, box.yMin, box.yMax));
            }
            
            
            Vector2 MoveTowardsBoxCenter(Vector2 position, float amount, Rect box)
            {
                Vector2 center = box.center;
                Vector2 direction = center - position;
                return position + direction.normalized * amount;
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
