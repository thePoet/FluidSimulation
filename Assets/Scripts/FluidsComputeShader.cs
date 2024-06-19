
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
            
            _computeShader.SetInt("_MaxNumParticles", settings.MaxNumParticles);
            _computeShader.SetInt("_MaxNumNeighbours", settings.MaxNumNeighbours);
            
            _computeShader.SetInt("_MaxNumParticlesPerCell", settings.MaxNumParticlesInPartitioningCell);
            _computeShader.SetFloat("_InteractionRadius", _settings.InteractionRadius);
        }

     

        public void Dispose()
        {
            ReleaseBuffers();
        }


        public void Step(float deltaTime, ParticleData data)
        {
            _computeShader.SetInt("_NumParticles", data.NumberOfParticles);
            _computeShader.SetFloat("_Time", deltaTime);
           
            WriteToBuffers(data);
            Execute(Kernel.ApplyVelocity, threadGroupsForParticles);
            ReadFromBuffers(data);
          
            
            data.UpdateNeighbours();
            WriteToBuffers(data);
            
            
            Execute(Kernel.CalculatePressures, threadGroupsForParticles);
            Execute(Kernel.CalculateDensityDisplacement, threadGroupsForParticles);
            Execute(Kernel.ApplyDensityDisplacement, threadGroupsForParticles);
            ReadFromBuffers(data);

      

        }

        
        private void CreateBuffers()
        {
            _particleBuffer = new ComputeBuffer(_settings.MaxNumParticles, FluidParticle.Stride);
            _particleNeighbours = new ComputeBuffer(_settings.MaxNumParticles * _settings.MaxNumNeighbours, sizeof(int));
            _particleNeighbourCount = new ComputeBuffer(_settings.MaxNumParticles , sizeof(int));
        }

        private void SetBuffers()
        {
            _computeShader.SetBuffer((int)Kernel.ApplyVelocity, "_Particles", _particleBuffer);
            _computeShader.SetBuffer((int)Kernel.CalculatePressures, "_Particles", _particleBuffer);
            _computeShader.SetBuffer((int)Kernel.CalculateDensityDisplacement, "_Particles", _particleBuffer);
            _computeShader.SetBuffer((int)Kernel.ApplyDensityDisplacement, "_Particles", _particleBuffer);



            _computeShader.SetBuffer((int)Kernel.CalculatePressures, "_ParticleNeighbours", _particleNeighbours);
            _computeShader.SetBuffer((int)Kernel.CalculatePressures, "_ParticleNeighbourCount",
                _particleNeighbourCount);
            _computeShader.SetBuffer((int)Kernel.CalculateDensityDisplacement, "_ParticleNeighbours",
                _particleNeighbours);
            _computeShader.SetBuffer((int)Kernel.CalculateDensityDisplacement, "_ParticleNeighbourCount",
                _particleNeighbourCount);

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
          //    _cellParticleCount.Release();
          //_particlesInCells.Release();
          _particleNeighbours.Release();
          _particleNeighbourCount.Release();
          //_statsBuffer.Release();
        }
        
        private void Execute(Kernel kernel, Vector3Int threadGroups)
        {
            _computeShader.Dispatch((int)kernel, threadGroups.x, threadGroups.y, threadGroups.z);
        }
        
        private Vector3Int threadGroupsForParticles => new Vector3Int(32, 16, 1);
        private Vector3Int threadGroupsForCells => new Vector3Int(32, 16, 1); //NOTE: This is not correct
    }
}
