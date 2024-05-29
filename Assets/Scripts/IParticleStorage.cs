using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    public interface IParticleData
    {
        int Add(FluidParticle particle);
        void Remove(int particleIndex);
        Span<FluidParticle> All();
        Span<int> NeighbourIndices(int particleIndex);
        void UpdateNeighbours();
        int NumberOfParticles { get; }
        int MaxNumberOfParticles { get; }
        
        void Clear();
   
        public ComputeBuffer CreateParticlesBuffer();
        public void WriteParticlesToBuffer(ComputeBuffer buffer);
        public void ReadParticlesFromBuffer(ComputeBuffer buffer);
        
        
        Dictionary<(int,int),float> Springs { get; }
    } 
    
    public interface IPositionAndId
    {
        Vector2 Position { get;  }
        int Id { get; }
    }
}