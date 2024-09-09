using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsComputeShader
   {
        

        private struct Buffers
        {
            private ComputeBuffer _particleDataRW;
            private ComputeBuffer _particleDataInternal;
            private ComputeBuffer _cellParticleCount;
            private ComputeBuffer _particlesInCells;
            private ComputeBuffer _particleNeighbours;
            private ComputeBuffer _particleNeighbourCount;
            private ComputeBuffer _fluids;
            private ComputeBuffer _stats;
            private ComputeBuffer _debug;
            
            public void Create(SimulationSettings settings, Fluid[] fluids)
            {
                _particleDataRW = new ComputeBuffer(settings.MaxNumParticles, FluidParticle.Stride, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                _particleDataInternal = new ComputeBuffer(settings.MaxNumParticles, InternalParticleData.Stride);
                _particleNeighbours = new ComputeBuffer(settings.MaxNumParticles * settings.MaxNumNeighbours, sizeof(int));
                _particleNeighbourCount = new ComputeBuffer(settings.MaxNumParticles , sizeof(int));
                int numCells = settings.PartitioningGrid.NumberOfSquares;
                _cellParticleCount = new ComputeBuffer(numCells, sizeof(int));
                _particlesInCells = new ComputeBuffer(settings.MaxNumParticlesInPartitioningCell*numCells, sizeof(int));
                _fluids = new ComputeBuffer(fluids.Length, Fluid.Stride);
                _fluids.SetData(fluids);
                _stats = new ComputeBuffer(10 , sizeof(int));
                _debug = new ComputeBuffer(10 , sizeof(float));
            }

            public void SetForAllKernels(ComputeShader computeShader, int numKernels)
            {
                SetBufferForAllKernels("_Particles", _particleDataRW);
                SetBufferForAllKernels("_TempParticleData", _particleDataInternal);
                SetBufferForAllKernels("_ParticleNeighbours", _particleNeighbours); 
                SetBufferForAllKernels("_ParticleNeighbourCount", _particleNeighbourCount);
                SetBufferForAllKernels("_CellParticleCount", _cellParticleCount); 
                SetBufferForAllKernels("_ParticlesInCells", _particlesInCells);
                SetBufferForAllKernels("_Fluids", _fluids);
                SetBufferForAllKernels("_Stats", _stats);   
                SetBufferForAllKernels("_Debug", _debug);          
            
                void SetBufferForAllKernels(string bufferName, ComputeBuffer buffer)
                {
                    for (int i = 0; i < numKernels; i++)
                    {
                        computeShader.SetBuffer(i, bufferName, buffer);
                    }
                }
            }
            
            public void Release()
            {
                _particleDataRW.Release();
                _particleDataInternal.Release();
                _cellParticleCount.Release();
                _particlesInCells.Release();
                _particleNeighbours.Release();
                _particleNeighbourCount.Release();
                _fluids.Release();
                _stats.Release();
                _debug.Release();
            }
            
            public void ReadParticleData(FluidParticle[] particles)
            {
                _particleDataRW.GetData(particles);  
            }

            public void WriteParticleData(FluidParticle[] particles)
            {
                _particleDataRW.SetData(particles);
            }

            public Vector2[] DebugData()
            {
                Vector2[] data = new Vector2[5];
                _debug.GetData(data);
                return data;
            }

            public int[] GetStats()
            {
                int[] data = new int[10];
                _stats.GetData(data);
                return data;
            }
        }
        
        
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
        
        private struct InternalParticleData
        {
            public Vector2 PositionChange;
            public Vector2 VelocityChange;
            public Vector2 PreviousPosition;
            public float Pressure;
            public float NearPressure;
            
            public static int Stride => 8 * sizeof(float);
        }
        

        public int SelectedParticle = 0;
        private readonly ComputeShader _computeShader;

        private Buffers _buffers;
        private readonly SimulationSettings _simulationSettings;
        
        public FluidsComputeShader(string shaderFileName, SimulationSettings simulationSettings, Fluid[] fluids)  
        {
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _simulationSettings = simulationSettings;
            _buffers.Create(simulationSettings, fluids);
            _buffers.SetForAllKernels(_computeShader, NumKernels);
            SetShaderVariables(simulationSettings);
        }

        
        public void Dispose()
        {
           _buffers.Release();
        }


        public Vector2[] GetSelectedParticleData() => _buffers.DebugData();
        
        

        public void Step(float deltaTime, FluidParticle[] particles, int numParticles)
        {
            float time = Time.realtimeSinceStartup;
            
            _computeShader.SetInt("_NumParticles", numParticles);
            _computeShader.SetFloat("_DeltaTime", deltaTime/_simulationSettings.NumSubSteps);
            _computeShader.SetFloat("_MaxDisplacement", _simulationSettings.InteractionRadius * 0.45f);
            _computeShader.SetFloat("_SolidRadius", 15f);
            _computeShader.SetInt("_SelectedParticle", SelectedParticle);
            
            _buffers.WriteParticleData(particles);

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
                
                Execute(Kernel.ApplyVelocity, threadGroupsForParticles); 
                
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
                
          //      PrintDebugData();
            }
            

            _buffers.ReadParticleData(particles);
            CheckErrorFlags();
          
            
            

           // Debug.Log("Sim step with read/write took " + 1000f * (Time.realtimeSinceStartup - time) + " ms.");

        }

        private int NumKernels => Enum.GetNames(typeof(Kernel)).Length;
        
        private void CheckErrorFlags()
        {
            var errorFlags = _buffers.GetStats();
            string prefix = "FluidsComputeShader Warning: ";
            if (errorFlags[0] > 0) Debug.LogWarning(prefix + "Too many particles in a cell: " + + errorFlags[0]);
            if (errorFlags[1] > 0) Debug.LogWarning(prefix + "Particles outside area: " + + errorFlags[1]);
            if (errorFlags[3] > 0) Debug.LogWarning(prefix + "Fluid particle starts inside solid: " + errorFlags[3]);
        }
        
        private void PrintDebugData()
        {
            Debug.Log(_buffers.DebugData()[0]);
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
        


  
        

        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); //NOTE: This is too many
    }
}
