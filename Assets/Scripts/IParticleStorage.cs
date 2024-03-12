using System;
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
        void Clear();
    } 
    
    public interface IPositionAndId
    {
        Vector2 Position { get;  }
        int Id { get; }
    }
}