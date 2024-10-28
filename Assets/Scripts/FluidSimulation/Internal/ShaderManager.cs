using System;
using System.Collections.Generic;
using UnityEngine;
using FluidDemo; // TODO: pois

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

        struct Variables
        {
            public int NumProximityAlerts;
            public int NumCellOverflowErrors;
            public int NumParticleOutsideAreaErrors;
            public int NumInsideSolidWarnings;
            public int ProximityAlerstMaxedOut;
            public static int Stride => 5 * sizeof(int);
        };
        
        
        public int SelectedParticle = -1;
        
        private readonly ComputeShader _computeShader;
        private ShaderBuffer[] _buffers;
        private readonly SimulationSettingsInternal _simulationSettings;
        private ProximityAlert[] _proximityAlerts;
        private int _maxNumProximityAlerts;
        
        public ShaderManager(string shaderFileName, SimulationSettingsInternal simulationSettings,
            FluidInternal[] fluids, ProximityAlertRequest[] alerts, int maxNumProxAlerts)  
        {
            _maxNumProximityAlerts = maxNumProxAlerts;
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _simulationSettings = simulationSettings;
            
            _buffers = CreateBuffers(simulationSettings, fluids, NumPartitioningCells());
            _buffers[6].ComputeBuffer.SetData(fluids);

         
            var alertMatrix = CreateProximityAlertMatrix(alerts, fluids);
            _proximityAlerts = new ProximityAlert[_maxNumProximityAlerts];
            
            _buffers[9].ComputeBuffer.SetData( alertMatrix );

            foreach (int kernelIndex in AllKernelIndices())
            {
                foreach (ShaderBuffer buffer in _buffers)
                {
                    _computeShader.SetBuffer(kernelIndex, buffer.Name, buffer.ComputeBuffer);
                }
            }

            SetShaderVariables(simulationSettings, fluids);
            
            // TODO: move this inside shader
            int NumPartitioningCells()
            {
                Rect area = simulationSettings.AreaBounds;
                float squareSize = simulationSettings.InteractionRadius;
                int result = Mathf.CeilToInt((area.max.x - area.min.x) / squareSize) *
                               Mathf.CeilToInt((area.max.y - area.min.y) / squareSize);
                return result;
            }
        }
        
       
        public void Step(float deltaTime, FluidSimParticle[] particles)
        {
            float time=Time.realtimeSinceStartup;

//            particles.WriteToComputeBuffer(_buffers[0].ComputeBuffer);
            _buffers[0].ComputeBuffer.SetData(particles);
            
            
       //     _computeShader.SetInt("_NumParticles", numParticles);
            _computeShader.SetFloat("_DeltaTime", deltaTime/_simulationSettings.NumSubSteps);
            _computeShader.SetFloat("_MaxDisplacement", _simulationSettings.InteractionRadius * 0.45f);
            _computeShader.SetInt("_SelectedParticle", SelectedParticle);
            
        
          
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

//            particles.ReadFromComputeBuffer(_buffers[0].ComputeBuffer);
            _buffers[0].ComputeBuffer.GetData(particles);  
            
            CheckErrorFlags();

            var variable = GetVariables();
        

            Vector2[] data = new Vector2[5];
            _buffers[8].ComputeBuffer.GetData(data); 
//            Debug.Log (data[0].ToString());
//           Debug.Log("Sim step with " + numParticles + " particles : " + 1000f * (Time.realtimeSinceStartup - time) + " ms.");
        }

        public Span<ProximityAlert> GetProximityAlerts()
        {
            _buffers[10].ComputeBuffer.GetData(_proximityAlerts);
            return _proximityAlerts.AsSpan(0, GetVariables().NumProximityAlerts);
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


        private void SetShaderVariables(SimulationSettingsInternal simulationSettings, FluidInternal[] fluids)
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
            _computeShader.SetInt("_NumFluids", fluids.Length);
            _computeShader.SetInt("_MaxNumProximityAlerts", _maxNumProximityAlerts);
        }

        
        private ShaderBuffer[] CreateBuffers(SimulationSettingsInternal settings, FluidInternal[] fluids,
            int numPartitioningCells)
        {
            var buffers = new ShaderBuffer[11];
            var s = settings;
            int numPart = s.MaxNumParticles;
            int numNeigh = s.MaxNumNeighbours;
            int numCells = numPartitioningCells;
            int numPartInCell = s.MaxNumParticlesInPartitioningCell;

            buffers[0] = new ShaderBuffer("_Particles",              numPart,                  FluidSimParticle.Stride, ShaderBuffer.Type.IO);
            buffers[1] = new ShaderBuffer("_TempData",               numPart,                  14 * sizeof(float),   ShaderBuffer.Type.Internal);
            buffers[2] = new ShaderBuffer("_ParticleNeighbours",     numPart * numNeigh,       sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[3] = new ShaderBuffer("_ParticleNeighbourCount", numPart,                  sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[4] = new ShaderBuffer("_CellParticleCount",      numCells,                 sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[5] = new ShaderBuffer("_ParticlesInCells",       numCells * numPartInCell, sizeof(int),          ShaderBuffer.Type.Internal);
            buffers[6] = new ShaderBuffer("_Fluids",                 fluids.Length,            FluidInternal.Stride, ShaderBuffer.Type.Internal);
            buffers[7] = new ShaderBuffer("_Variables",              1,                        Variables.Stride,     ShaderBuffer.Type.IO);
            buffers[8] = new ShaderBuffer("_Debug",                  10,                       sizeof(float),        ShaderBuffer.Type.IO);
            buffers[9] = new ShaderBuffer("_ProximityAlertMatrix",   fluids.Length*fluids.Length, sizeof(float),     ShaderBuffer.Type.Internal); 
            buffers[10] = new ShaderBuffer("_ProximityAlerts",       _maxNumProximityAlerts,    sizeof(int)*2,        ShaderBuffer.Type.IO); 
            
            return buffers;
        }


        private float[] CreateProximityAlertMatrix(ProximityAlertRequest[] alerts, FluidInternal[] fluids)
        {
            float[] result = new float[fluids.Length * fluids.Length];
            Array.Fill(result, -123f);
   
            if (alerts == null) return result;
         
            foreach (var alert in alerts)
            {
                CheckFluidIndex(alert.IndexFluidA);
                CheckFluidIndex(alert.IndexFluidB);

                int index = alert.IndexFluidA + alert.IndexFluidB * fluids.Length;
                result[index] = alert.Range;
            }
            
            return result;
            
            void CheckFluidIndex(int idx)
            {
                if (idx < 0 || idx >= fluids.Length) throw new IndexOutOfRangeException("Fluid index out of range.");
            }

        }
        
        private void CheckErrorFlags()
        {
            var v = GetVariables();
            
            string prefix = "FluidsComputeShader Warning: ";
            if (v.NumCellOverflowErrors > 0) Debug.LogWarning(prefix + "Too many particles in a cell: " + + v.NumCellOverflowErrors);
            if (v.NumParticleOutsideAreaErrors > 0) Debug.LogWarning(prefix + "Particles outside area: " + + v.NumParticleOutsideAreaErrors);
//            if (v.NumInsideSolidWarnings > 0) Debug.LogWarning(prefix + "Fluid particle starts inside solid: " + v.NumInsideSolidWarnings);
            if (v.ProximityAlerstMaxedOut > 0) Debug.LogWarning(prefix + "Proximity alerts maxed out.");
//       
        }

        private Variables GetVariables()
        {
            var variables = new Variables[1];
            _buffers[7].ComputeBuffer.GetData(variables);
            return variables[0];
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
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1); // TODO: Calculate necessary amount
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); // TODO: Calculate necessary amount, this is too many.
    }
}


