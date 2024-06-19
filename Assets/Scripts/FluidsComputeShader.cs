
using System;
using UnityEngine;

namespace FluidSimulation
{
   public class FluidsComputeShader
    {
        
        private ComputeShader _computeShader;
        
        private ComputeBuffer _particleBuffer;
        private ComputeBuffer _cellParticleCount;
        private ComputeBuffer _particlesInCells;
        private ComputeBuffer _particleNeighbours;
        private ComputeBuffer _particleNeighbourCount;
        private ComputeBuffer _statsBuffer;
      


        enum Kernel
        {
            ClearPartitioning = 0,
            FillPartitioning = 1,
            FindNeighbours = 2,
            CalculateViscosity = 3,
            ApplyViscosity = 4,
            ApplyVelocity = 5,
            CalculatePressures = 6,
            CalculateDensityDisplacement = 7,
            ApplyDensityDisplacement = 8
        }

        private readonly ParticleDynamics.Settings _settings;
        
        public FluidsComputeShader(string shaderFileName, ParticleDynamics.Settings settings)  
        {
            _computeShader = Resources.Load(shaderFileName) as ComputeShader;
            if (_computeShader == null)
            {
                throw new Exception("Could not load compute shader: " + shaderFileName);
            }
            
            _settings = settings;
            
            CreateBuffers();
            SetBuffers();
            
            SetShaderVariables(settings);
        }




        public void Dispose()
        {
            ReleaseBuffers();
        }


        public void Step(float deltaTime, ParticleData data)
        {
           

            _computeShader.SetInt("_NumParticles", data.NumberOfParticles);
            _computeShader.SetFloat("_Time", deltaTime);
            float t0 = Time.realtimeSinceStartup;
            WriteToBuffers(data);
            float t1 = Time.realtimeSinceStartup;
            Execute(Kernel.CalculateViscosity, threadGroupsForParticles);
            Execute(Kernel.ApplyViscosity, threadGroupsForParticles);
            Execute(Kernel.ApplyVelocity, threadGroupsForParticles);

            float t2 = Time.realtimeSinceStartup;
            Execute(Kernel.ClearPartitioning, threadGroupsForCells);
            Execute(Kernel.FillPartitioning, threadGroupsForParticles);
            Execute(Kernel.FindNeighbours, threadGroupsForParticles);
            float t3 = Time.realtimeSinceStartup;

            for (int i = 0; i < 3; i++)
            {
                Execute(Kernel.CalculatePressures, threadGroupsForParticles);
                Execute(Kernel.CalculateDensityDisplacement, threadGroupsForParticles);
                Execute(Kernel.ApplyDensityDisplacement, threadGroupsForParticles);
            }

            float t4 = Time.realtimeSinceStartup;
            ReadFromBuffers(data);
            float t5 = Time.realtimeSinceStartup;

            Debug.Log("TOTAL " + 1000f * (t5 - t0) + " ms. ");
        }

        private void SetShaderVariables(ParticleDynamics.Settings settings)
        {
            _computeShader.SetInt("_MaxNumParticles", settings.MaxNumParticles);
            _computeShader.SetInt("_MaxNumNeighbours", settings.MaxNumNeighbours);
            
            _computeShader.SetInt("_MaxNumParticlesPerCell", settings.MaxNumParticlesInPartitioningCell);
            _computeShader.SetFloat("_InteractionRadius", _settings.InteractionRadius);
            
            _computeShader.SetFloat("_AreaMinX", settings.AreaBounds.xMin);
            _computeShader.SetFloat("_AreaMinY", settings.AreaBounds.yMin);
            _computeShader.SetFloat("_AreaMaxX", settings.AreaBounds.xMax);
            _computeShader.SetFloat("_AreaMaxY", settings.AreaBounds.yMax);
        }
        
        private void CreateBuffers()
        {
            _particleBuffer = new ComputeBuffer(_settings.MaxNumParticles, FluidParticle.Stride);
            _particleNeighbours = new ComputeBuffer(_settings.MaxNumParticles * _settings.MaxNumNeighbours, sizeof(int));
            _particleNeighbourCount = new ComputeBuffer(_settings.MaxNumParticles , sizeof(int));

            int numCells = _settings.PartitioningGrid.NumberOfCells;
            _cellParticleCount = new ComputeBuffer(numCells, sizeof(int));
            _particlesInCells = new ComputeBuffer(_settings.MaxNumParticlesInPartitioningCell*numCells, sizeof(int));

            _statsBuffer = new ComputeBuffer(10 , sizeof(float));
        }

        private void SetBuffers()
        {
            SetBufferForAllKernels("_Particles", _particleBuffer);
            SetBufferForAllKernels("_ParticleNeighbours", _particleNeighbours); 
            SetBufferForAllKernels("_ParticleNeighbourCount", _particleNeighbourCount);
            SetBufferForAllKernels("_CellParticleCount", _cellParticleCount); 
            SetBufferForAllKernels("_ParticlesInCells", _particlesInCells);
            SetBufferForAllKernels("_Stats", _statsBuffer);          
            
            void SetBufferForAllKernels(string bufferName, ComputeBuffer buffer)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Kernel)).Length; i++)
                {
                    _computeShader.SetBuffer(i, bufferName, buffer);
                }
            }
        }

        private void ReadFromBuffers(ParticleData data)
        {
            data.ReadParticlesFromBuffer(_particleBuffer);
        }

        private void WriteToBuffers(ParticleData data)
        {
            data.WriteParticlesToBuffer(_particleBuffer);
            data.WriteNeighboursToBuffer(_particleNeighbours, _particleNeighbourCount);
        }
        
        
        private void ReleaseBuffers()
        {
            _particleBuffer.Release(); 
            _cellParticleCount.Release();
          _particlesInCells.Release();
          _particleNeighbours.Release();
          _particleNeighbourCount.Release();
          _statsBuffer.Release();
        }
        
        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); //NOTE: This is not correct
    }
}
