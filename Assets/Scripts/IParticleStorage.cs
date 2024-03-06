using System;
using UnityEngine;

namespace FluidSimulation
{
    public interface IParticleStorage
    {
        int Add(FluidParticle particle);
        void Remove(int particleId);
      
        Span<FluidParticle> All();
        Span<int> NeighboursOf(int particleId);
        Span<(int, int)> NeighbourParticlePairs();
       
    } 
    
    public interface ISpatiallyPartible
    {
        Vector2 Position { get;  }
        int Id { get; }
    }
}