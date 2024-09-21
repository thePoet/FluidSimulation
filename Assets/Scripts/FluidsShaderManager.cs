using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsShaderManager
   {
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
   

        public int SelectedParticle = 0;
        
        private readonly ComputeShader _computeShader;
        private ShaderBuffer[] _buffers;
        private readonly SimulationSettings _simulationSettings;
        
        public FluidsShaderManager(string shaderFileName, SimulationSettings simulationSettings, Fluid[] fluids)  
        {
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _simulationSettings = simulationSettings;
            _buffers = CreateBuffers(simulationSettings, fluids);
            
            _buffers[6].ComputeBuffer.SetData(fluids);

            foreach (int kernelIndex in AllKernelIndices())
            {
                foreach (ShaderBuffer buffer in _buffers)
                {
                    _computeShader.SetBuffer(kernelIndex, buffer.Name, buffer.ComputeBuffer);
                }
            }
            
            SetShaderVariables(simulationSettings);
        }

        
       
        public void Step(float deltaTime, FluidParticle[] particles, int numParticles)
        {
            _computeShader.SetInt("_NumParticles", numParticles);
            _computeShader.SetFloat("_DeltaTime", deltaTime/_simulationSettings.NumSubSteps);
            _computeShader.SetFloat("_MaxDisplacement", _simulationSettings.InteractionRadius * 0.45f);
            _computeShader.SetFloat("_SolidRadius", 15f);
            _computeShader.SetInt("_SelectedParticle", SelectedParticle);
            
            _buffers[0].ComputeBuffer.SetData(particles);

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
            }
   
            _buffers[0].ComputeBuffer.GetData(particles);  
            CheckErrorFlags();
   
           // Debug.Log("Sim step with read/write took " + 1000f * (Time.realtimeSinceStartup - time) + " ms.");
        }

   
   
        public void Dispose()
        {
            foreach (ShaderBuffer buffer in _buffers) buffer.ComputeBuffer.Release();
        }


        public Vector2[] GetSelectedParticleData()
        {
            Vector2[] data = new Vector2[5];
            _buffers[8].ComputeBuffer.GetData(data); 
            return data;
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

        
        private ShaderBuffer[] CreateBuffers(SimulationSettings settings, Fluid[] fluids)
        {
            var buffers = new ShaderBuffer[9];
            var s = settings;
            int numPart = s.MaxNumParticles;
            int numNeigh = s.MaxNumNeighbours;
            int numCells = s.PartitioningGrid.NumberOfSquares;
            int numPartInCell = s.MaxNumParticlesInPartitioningCell;

            buffers[0] = new ShaderBuffer("_Particles",              numPart,                  FluidParticle.Stride, ShaderBuffer.Type.IO);
            buffers[1] = new ShaderBuffer("_TempParticleData",       numPart,                  8 * sizeof(float),    ShaderBuffer.Type.Internal);
            buffers[2] = new ShaderBuffer("_ParticleNeighbours",     numPart * numNeigh,       sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[3] = new ShaderBuffer("_ParticleNeighbourCount", numPart,                  sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[4] = new ShaderBuffer("_CellParticleCount",      numCells,                 sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[5] = new ShaderBuffer("_ParticlesInCells",       numCells * numPartInCell, sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[6] = new ShaderBuffer("_Fluids",                 fluids.Length,            Fluid.Stride,         ShaderBuffer.Type.Internal);
            buffers[7] = new ShaderBuffer("_Stats",                  10,                       sizeof(int),          ShaderBuffer.Type.IO);
            buffers[8] = new ShaderBuffer("_Debug",                  10,                       sizeof(float),        ShaderBuffer.Type.IO);
            
            return buffers;
        }
        
        private void CheckErrorFlags()
        {
            int[] errorFlags = new int[10];
            _buffers[7].ComputeBuffer.GetData(errorFlags);
            
            string prefix = "FluidsComputeShader Warning: ";
            if (errorFlags[0] > 0) Debug.LogWarning(prefix + "Too many particles in a cell: " + + errorFlags[0]);
            if (errorFlags[1] > 0) Debug.LogWarning(prefix + "Particles outside area: " + + errorFlags[1]);
            if (errorFlags[3] > 0) Debug.LogWarning(prefix + "Fluid particle starts inside solid: " + errorFlags[3]);
        }
        
        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }

        private IEnumerable<int> AllKernelIndices()
        {
            int mumKernels = Enum.GetNames(typeof(Kernel)).Length;
            
            for (int kernelIndex = 0; kernelIndex < mumKernels; kernelIndex++)
            {
                yield return kernelIndex;
            }
        }
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); //NOTE: This is too many
    }
}


