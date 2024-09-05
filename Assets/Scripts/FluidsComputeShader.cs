using System;
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsComputeShader
    {
        private struct TempParticleData
        {
            public Vector2 PositionChange;
            public Vector2 VelocityChange;
            public Vector2 PreviousPosition;
            public float Pressure;
            public float NearPressure;
            
            public static int Stride => 8 * sizeof(float);
        }

        private readonly ComputeShader _computeShader;
        
        private ComputeBuffer _particleBuffer;
        private ComputeBuffer _tempParticleData;
        private ComputeBuffer _cellParticleCount;
        private ComputeBuffer _particlesInCells;
        private ComputeBuffer _particleNeighbours;
        private ComputeBuffer _particleNeighbourCount;
        private ComputeBuffer _fluidsBuffer;
        private ComputeBuffer _statsBuffer;
        
        enum Kernel
        {
            ApplyGravity = 0,
            ClearPartitioning = 1,
            FillPartitioning = 2,
            FindNeighbours = 3,
            CalculateViscosity = 4,
            ApplyViscosity = 5,
            ApplyVelocity = 6,
            CalculatePressures = 7,
            CalculateDensityDisplacement = 8,
            CalculateCollisionDisplacement = 9,
            ApplyDisplacement = 10,
            ConfineParticlesToArea = 11,
            CalculateVelocityBasedOnMovement = 12
        }

        private readonly SimulationSettings _simulationSettings;
        private readonly Fluid[] _fluids;
        
        public FluidsComputeShader(string shaderFileName, SimulationSettings simulationSettings, Fluid[] fluids)  
        {
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _simulationSettings = simulationSettings;
            _fluids = fluids;
            
            CreateBuffers();
            SetBuffers();
            SetShaderVariables(simulationSettings);
        }

        
        public void Dispose()
        {
            ReleaseBuffers();
        }


        public void Step(float deltaTime, FluidParticle[] particles, int numParticles)
        {
            float time = Time.realtimeSinceStartup;
            
            _computeShader.SetInt("_NumParticles", numParticles);
            _computeShader.SetFloat("_DeltaTime", deltaTime/_simulationSettings.NumSubSteps);
            _computeShader.SetFloat("_MaxDisplacement", _simulationSettings.InteractionRadius * 0.45f);
            _computeShader.SetFloat("_SolidRadius", 15f);
            
            WriteToBuffers(particles);

            for (int s = 0; s < _simulationSettings.NumSubSteps; s++)
            {
                
                //adjust velocity
                Execute(Kernel.ApplyGravity, threadGroupsForParticles);

                if (_simulationSettings.IsViscosityEnabled)
                {
                    //adjust velocityChange
                    Execute(Kernel.CalculateViscosity, threadGroupsForParticles);
                    // velocity += velocityChange 
                   Execute(Kernel.ApplyViscosity, threadGroupsForParticles);
                }

                
                Execute(Kernel.ApplyVelocity, threadGroupsForParticles); //->ApplyDisplacement
                
                //   Partitioning based on Position
                Execute(Kernel.ClearPartitioning, threadGroupsForCells);
                Execute(Kernel.FillPartitioning, threadGroupsForParticles);
                Execute(Kernel.FindNeighbours, threadGroupsForParticles);
                
                Execute(Kernel.ConfineParticlesToArea, threadGroupsForParticles);
                
                Execute(Kernel.CalculatePressures, threadGroupsForParticles);
                Execute(Kernel.CalculateDensityDisplacement, threadGroupsForParticles);
                Execute(Kernel.CalculateCollisionDisplacement, threadGroupsForParticles);
                Execute(Kernel.ApplyDisplacement, threadGroupsForParticles);
           
                Execute(Kernel.ConfineParticlesToArea, threadGroupsForParticles);
                Execute(Kernel.CalculateVelocityBasedOnMovement, threadGroupsForParticles);
            }
            

            ReadFromBuffers(particles);
            CheckErrorFlags();
            
            

           // Debug.Log("Sim step with read/write took " + 1000f * (Time.realtimeSinceStartup - time) + " ms.");

        }

        private void CheckErrorFlags()
        {
            float[] errorFlags = new float[10];
            _statsBuffer.GetData(errorFlags);
            string prefix = "FluidsComputeShader Warning: ";
            if (errorFlags[0] > 0f) Debug.LogWarning(prefix + "Too many particles in a cell: " + + errorFlags[0]);
            if (errorFlags[1] > 0f) Debug.LogWarning(prefix + "Paricles outside area: " + + errorFlags[0]);
            if (errorFlags[3] > 0f) Debug.LogWarning(prefix + "Fluid particle starts inside solid: " + errorFlags[3]);
        }
    

        private void SetShaderVariables(SimulationSettings simulationSettings)
        {
            _computeShader.SetInt("_MaxNumParticles", simulationSettings.MaxNumParticles);
            _computeShader.SetInt("_MaxNumNeighbours", simulationSettings.MaxNumNeighbours);
            
            _computeShader.SetInt("_MaxNumParticlesPerCell", simulationSettings.MaxNumParticlesInPartitioningCell);
            _computeShader.SetFloat("_InteractionRadius", _simulationSettings.InteractionRadius);
            
            _computeShader.SetFloat("_AreaMinX", simulationSettings.AreaBounds.xMin);
            _computeShader.SetFloat("_AreaMinY", simulationSettings.AreaBounds.yMin);
            _computeShader.SetFloat("_AreaMaxX", simulationSettings.AreaBounds.xMax);
            _computeShader.SetFloat("_AreaMaxY", simulationSettings.AreaBounds.yMax);
            
            _computeShader.SetFloat("_Gravity", simulationSettings.Gravity);
            _computeShader.SetFloat("_Drag", simulationSettings.Drag);
        }
        
        private void CreateBuffers()
        {
            _particleBuffer = new ComputeBuffer(_simulationSettings.MaxNumParticles, FluidParticle.Stride, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            _tempParticleData = new ComputeBuffer(_simulationSettings.MaxNumParticles, TempParticleData.Stride);
            
            _particleNeighbours = new ComputeBuffer(_simulationSettings.MaxNumParticles * _simulationSettings.MaxNumNeighbours, sizeof(int));
            _particleNeighbourCount = new ComputeBuffer(_simulationSettings.MaxNumParticles , sizeof(int));

            int numCells = _simulationSettings.PartitioningGrid.NumberOfSquares;
            _cellParticleCount = new ComputeBuffer(numCells, sizeof(int));
            _particlesInCells = new ComputeBuffer(_simulationSettings.MaxNumParticlesInPartitioningCell*numCells, sizeof(int));
            
            _fluidsBuffer = new ComputeBuffer(_fluids.Length, Fluid.Stride);
            _fluidsBuffer.SetData(_fluids);

            _statsBuffer = new ComputeBuffer(10 , sizeof(float));
        }

        private void SetBuffers()
        {
            SetBufferForAllKernels("_Particles", _particleBuffer);
            SetBufferForAllKernels("_TempParticleData", _tempParticleData);
            SetBufferForAllKernels("_ParticleNeighbours", _particleNeighbours); 
            SetBufferForAllKernels("_ParticleNeighbourCount", _particleNeighbourCount);
            SetBufferForAllKernels("_CellParticleCount", _cellParticleCount); 
            SetBufferForAllKernels("_ParticlesInCells", _particlesInCells);
            
            SetBufferForAllKernels("_Fluids", _fluidsBuffer);
            
            SetBufferForAllKernels("_Stats", _statsBuffer);          
            
            void SetBufferForAllKernels(string bufferName, ComputeBuffer buffer)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Kernel)).Length; i++)
                {
                    _computeShader.SetBuffer(i, bufferName, buffer);
                }
            }
        }

        private void ReadFromBuffers(FluidParticle[] particles)
        {
            _particleBuffer.GetData(particles);  
        }

        private void WriteToBuffers(FluidParticle[] particles)
        {
            _particleBuffer.SetData(particles);
        }
        
        
        private void ReleaseBuffers()
        {
            _particleBuffer.Release();
            _tempParticleData.Release();
            _cellParticleCount.Release();
            _particlesInCells.Release();
            _particleNeighbours.Release();
            _particleNeighbourCount.Release();
            _fluidsBuffer.Release();
            _statsBuffer.Release();
        }
        
        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); //NOTE: This is too many
    }
}
