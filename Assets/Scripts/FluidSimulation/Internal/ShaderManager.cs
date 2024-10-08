using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation.Internal
{
   public class ShaderManager
   {
        enum Kernel
        {
            InitAndApplyGravity = 0,
            ClearPartitioning = 1,
            FillPartitioning = 2,
            FindNeighbours = 3,
            CalculateViscosity = 4,
            ApplyViscosity = 5,
            ApplyVelocity = 6,
            CalculatePressures = 7,
            CalculateDensityDisplacement = 8,
            MoveParticles = 9
        }
        
        public int SelectedParticle = -1;
        
        private readonly ComputeShader _computeShader;
        private ShaderBuffer[] _buffers;
        private readonly SimulationSettingsInternal _simulationSettings;
        
        public ShaderManager(string shaderFileName, SimulationSettingsInternal simulationSettings, FluidInternal[] fluids, int numPartitioningCells)  
        {
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _simulationSettings = simulationSettings;
        
            _buffers = CreateBuffers(simulationSettings, fluids, numPartitioningCells);
            
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
        
       
        public void Step(float deltaTime, FluidParticles particles, int numParticles)
        {
            float time=Time.realtimeSinceStartup;
            
            _computeShader.SetInt("_NumParticles", numParticles);
            _computeShader.SetFloat("_DeltaTime", deltaTime/_simulationSettings.NumSubSteps);
            _computeShader.SetFloat("_MaxDisplacement", _simulationSettings.InteractionRadius * 0.45f);
            _computeShader.SetInt("_SelectedParticle", SelectedParticle);
            
            particles.WriteToComputeBuffer(_buffers[0].ComputeBuffer);
          
            for (int s = 0; s < _simulationSettings.NumSubSteps; s++)
            {
                Execute(Kernel.InitAndApplyGravity, threadGroupsForParticles);

                if (_simulationSettings.IsViscosityEnabled)
                {
                    Execute(Kernel.CalculateViscosity, threadGroupsForParticles);
                    Execute(Kernel.ApplyViscosity, threadGroupsForParticles);
                }
                Execute(Kernel.ApplyVelocity, threadGroupsForParticles); 
                Execute(Kernel.ClearPartitioning, threadGroupsForCells);
                Execute(Kernel.FillPartitioning, threadGroupsForParticles);
                Execute(Kernel.FindNeighbours, threadGroupsForParticles);
                Execute(Kernel.CalculatePressures, threadGroupsForParticles);
                Execute(Kernel.CalculateDensityDisplacement, threadGroupsForParticles);
                Execute(Kernel.MoveParticles, threadGroupsForParticles);
            }

            particles.ReadFromComputeBuffer(_buffers[0].ComputeBuffer);
            
            CheckErrorFlags();
   
//           Debug.Log("Sim step with " + numParticles + " particles : " + 1000f * (Time.realtimeSinceStartup - time) + " ms.");
        }
        
        public void Dispose()
        {
            foreach (ShaderBuffer buffer in _buffers) buffer.ComputeBuffer.Release();
        }


        public Vector2[] GetSelectedParticleData()
        {
            if (SelectedParticle == -1) return null;
            if (!_buffers[8].ComputeBuffer.IsValid()) return null;
            
            Vector2[] data = new Vector2[5];
            _buffers[8].ComputeBuffer.GetData(data); 
            return data;
        }


        private void SetShaderVariables(SimulationSettingsInternal simulationSettings)
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
            _computeShader.SetFloat("_SolidRadius", simulationSettings.SolidRadius);
        }

        
        private ShaderBuffer[] CreateBuffers(SimulationSettingsInternal settings, FluidInternal[] fluids, int numPartitioningCells)
        {
            var buffers = new ShaderBuffer[9];
            var s = settings;
            int numPart = s.MaxNumParticles;
            int numNeigh = s.MaxNumNeighbours;
            int numCells = numPartitioningCells;
            int numPartInCell = s.MaxNumParticlesInPartitioningCell;

            buffers[0] = new ShaderBuffer("_Particles",              numPart,                  FluidParticle.Stride, ShaderBuffer.Type.IO);
            buffers[1] = new ShaderBuffer("_TempData",               numPart,                  14 * sizeof(float),   ShaderBuffer.Type.Internal);
            buffers[2] = new ShaderBuffer("_ParticleNeighbours",     numPart * numNeigh,       sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[3] = new ShaderBuffer("_ParticleNeighbourCount", numPart,                  sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[4] = new ShaderBuffer("_CellParticleCount",      numCells,                 sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[5] = new ShaderBuffer("_ParticlesInCells",       numCells * numPartInCell, sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[6] = new ShaderBuffer("_Fluids",                 fluids.Length,            FluidInternal.Stride,         ShaderBuffer.Type.Internal);
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


