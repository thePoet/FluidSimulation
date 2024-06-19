
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
            SetBuffets();
        }

        private void SetBuffets()
        {
            _computeShader.SetBuffer((int)Kernel.ApplyVelocity, "_Particles", _particleBuffer);
        }

        ~FluidsComputeShader()
        {
            ReleaseBuffers();
        }


        public void Step(float deltaTime, ParticleData data)
        {
            _computeShader.SetInt("_NumParticles", data.NumberOfParticles);
            _computeShader.SetFloat("_Time", deltaTime);
            WriteToBuffers(data);
            Execute(Kernel.ApplyVelocity, threadGroupsParticles);
            ReadFromBuffers(data);
        }

        private void ReadFromBuffers(ParticleData data)
        {
            data.ReadParticlesFromBuffer(_particleBuffer);
        }

        private void WriteToBuffers(ParticleData data)
        {
            data.WriteParticlesToBuffer(_particleBuffer);
        }

        private void CreateBuffers()
        {
            _particleBuffer = new ComputeBuffer(_settings.MaxNumParticles, FluidParticle.Stride);
        }
        
        private void ReleaseBuffers()
        {
            _particleBuffer.Release();
            /*   _cellParticleCount.Release();
          _particlesInCells.Release();
          _particleNeighbours.Release();
          _particleNeighbourCount.Release();
          _statsBuffer.Release();*/
        }
        
        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }
        
        private Vector3Int threadGroupsParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsCells => new Vector3Int(32, 16, 1); //NOTE: This is not correct
    }
}
