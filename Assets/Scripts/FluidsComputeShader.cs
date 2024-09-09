using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsComputeShader
   {
        private struct Buffers
        {
            private class Buffer
            {
                public string name;
                public ComputeBuffer buffer;
                public Buffer(){}
            }
  
            private Buffer[] _buffers;
            
            public void Create(SimulationSettings settings, Fluid[] fluids)
            {
                _buffers = new Buffer[9];
                var s = settings;

                _buffers[0] = new Buffer
                {
                    name = "_Particles",
                    buffer = new ComputeBuffer(s.MaxNumParticles, FluidParticle.Stride,
                        ComputeBufferType.Default, ComputeBufferMode.Dynamic)
                };
                _buffers[1] = new Buffer
                {
                    name = "_TempParticleData",
                    buffer = new ComputeBuffer(s.MaxNumParticles, InternalParticleData.Stride)
                };
                _buffers[2] = new Buffer
                {
                    name = "_ParticleNeighbours",
                    buffer = new ComputeBuffer(s.MaxNumParticles * s.MaxNumNeighbours, sizeof(int))
                };
                _buffers[3] = new Buffer
                {
                    name = "_ParticleNeighbourCount",
                    buffer = new ComputeBuffer(s.MaxNumParticles, sizeof(int))
                };
                _buffers[4] = new Buffer
                {
                    name = "_CellParticleCount",
                    buffer = new ComputeBuffer(s.PartitioningGrid.NumberOfSquares, sizeof(int))
                };
                _buffers[5] = new Buffer
                {
                    name = "_ParticlesInCells",
                    buffer = new ComputeBuffer(s.PartitioningGrid.NumberOfSquares * s.MaxNumParticlesInPartitioningCell, sizeof(int))
                };
                _buffers[6] = new Buffer
                {
                    name = "_Fluids",
                    buffer = new ComputeBuffer(fluids.Length, Fluid.Stride)
                };
                _buffers[7] = new Buffer
                {
                    name = "_Stats",
                    buffer = new ComputeBuffer(10 , sizeof(int))
                };
                _buffers[8] = new Buffer
                {
                    name = "_Debug",
                    buffer = new ComputeBuffer(10 , sizeof(float))
                };
                
                _buffers[6].buffer.SetData(fluids);
            
            }

            public void SetForAllKernels(ComputeShader computeShader, int numKernels)
            {
                foreach (Buffer b in _buffers)
                {
                    for (int i = 0; i < numKernels; i++)
                    {
                        computeShader.SetBuffer(i, b.name, b.buffer);
                    }
                }
            }
            
            public void Release()
            {
                foreach (Buffer b in _buffers) b.buffer.Release();
            }
            
            public void ReadParticleData(FluidParticle[] particles)
            {
                _buffers[0].buffer.GetData(particles);  
            }

            public void WriteParticleData(FluidParticle[] particles)
            {
                _buffers[0].buffer.SetData(particles);
            }

            public Vector2[] DebugData()
            {
                Vector2[] data = new Vector2[5];
                _buffers[8].buffer.GetData(data); 
                return data;
            }

            public int[] GetStats()
            {
                int[] data = new int[10];
                _buffers[7].buffer.GetData(data); 
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


