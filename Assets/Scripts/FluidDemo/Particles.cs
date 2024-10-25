using System;
using System.Collections.Generic;
using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public class Particles
    {
        public int MaxNumParticles { get; init; }
        public int NumParticles { get; private set; }

        private FluidSimParticle[] _particles;
        private int _nextId = 0;
        private SpatialPartitioningGrid<int> _spatialPartitioning;

        public Particles(int maxNumParticles, SpatialPartitioningGrid<int> partitioning)
        {
            MaxNumParticles = maxNumParticles;
            NumParticles = 0;
            _particles = new FluidSimParticle[maxNumParticles];
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new FluidSimParticle
                {
                    Position = Vector2.zero,
                    Velocity = Vector2.zero,
                    SubstanceIndex = -1,
                    Id = -1,
                    Active = false
                };
            }
            _spatialPartitioning = partitioning;
        }
        
        public FluidSimParticle this[int index] 
        {
            get => _particles[index];
            set => _particles[index] = value;
        }
       
        public Span<FluidSimParticle> Span => _particles.AsSpan().Slice(0, NumParticles);


        public int Add(FluidSimParticle fluidSimParticle)
        {
            //TODO: kunnolla
            fluidSimParticle.Id = _nextId;
            _nextId++;
            NumParticles++;
            int index = NumParticles - 1;
            _particles[index] = fluidSimParticle;
            
            _spatialPartitioning.Add(index, fluidSimParticle.Position);
            
            return fluidSimParticle.Id;
        }

        public void Remove(int id)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            NumParticles = 0;
        }

        // Returns proximity alerts
        public List<(FluidId, FluidId)> SimulateFluids(float timestep, FluidDynamics fluidDynamics)
        {
            fluidDynamics.Step(0.015f, _particles);
            return null;
        }
        
        public int[] InsideRectangle(Rect rect) => _spatialPartitioning.RectangleContents(rect);
        
        public int[] InsideCircle(Vector2 position, float radius) => _spatialPartitioning.CircleContents(position, radius);

      
        public void UpdateSpatialPartitioningGrid()
        {
            _spatialPartitioning.Clear();
            for (int i = 0; i < NumParticles; i++)
            {
                if (_particles[i].Active) _spatialPartitioning.Add(i, _particles[i].Position);
            }
        }
        
 


    }
}