using System;
using Unity.Collections;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{
    public class ParticleData 
    {
        public int MaxNumberOfParticles { get; }
        private readonly int _maxNumNeighbours;
        private readonly int _maxNumParticlesInSpatialCell;
        private int _numParticles = 0;
        
        private readonly float _neighbourRadius;
        private readonly FluidParticle[] _particles;
        
        
        private int _nextId = 0;
        private readonly NeighbourSearch _neighbourSearch;
        private readonly Rect _bounds;

      
        private SpatialPartitioningGrid<int> _partitioningGrid;


        private int[] _neighbourIndices;
        private int[] _neighbourCount;
        
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleData(ParticleDynamics.Settings settings)
        {
            MaxNumberOfParticles = settings.MaxNumParticles;
            _maxNumNeighbours = settings.MaxNumNeighbours;
            _neighbourRadius = settings.InteractionRadius;
            _maxNumParticlesInSpatialCell = _maxNumNeighbours * 2;
            _bounds = settings.AreaBounds;
            
            _particles = new FluidParticle[MaxNumberOfParticles];
            _neighbourSearch = new NeighbourSearch(_neighbourRadius, MaxNumberOfParticles, _maxNumNeighbours);
           
           
            Rect gridBounds = new Rect(_bounds.min - Vector2.one * 2f * _neighbourRadius, 
                _bounds.size + 4f*Vector2.one * _neighbourRadius);
            
            var grid = new Grid2D(gridBounds, squareSize : _neighbourRadius);
            _partitioningGrid = new SpatialPartitioningGrid<int>(grid,  maxNumEntitiesInSquare: 25);
         
            
            
            _neighbourIndices = new int[MaxNumberOfParticles * _maxNumNeighbours];
            _neighbourCount = new int[MaxNumberOfParticles];
           
           
        }

        public ComputeBuffer CreateParticlesBuffer() => new ComputeBuffer(MaxNumberOfParticles, FluidParticle.Stride);
        public void WriteParticlesToBuffer(ComputeBuffer buffer)
        {
            // Faster(?) alternative to buffer.SetData(_particles):
            NativeArray<FluidParticle> na = buffer.BeginWrite<FluidParticle>(0, MaxNumberOfParticles);
            na.CopyFrom(_particles);
            buffer.EndWrite<FluidParticle>(MaxNumberOfParticles);
        }

        public void ReadParticlesFromBuffer(ComputeBuffer buffer)
        {
            buffer.GetData(_particles);  
        }
        
        public void ReadNeighboursFromBuffer(ComputeBuffer particleNeighbours, ComputeBuffer particleNeighbourCount)
        {
           particleNeighbours.GetData(_neighbourIndices);
           particleNeighbourCount.GetData(_neighbourCount);
        }
 
        
        public ComputeBuffer CreateSpatialBuffer() => new ComputeBuffer(MaxNumberOfParticles, FluidParticle.Stride);
        public void WriteSpatialToBuffer(ComputeBuffer buffer) => buffer.SetData(_particles);
        public void ReadSpatialFromBuffer(ComputeBuffer buffer) => buffer.GetData(_particles);


        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.Id = _nextId;
            _nextId++;
            _numParticles++;
            int index = _numParticles - 1;
            _particles[index] = particle;
            
            return particle.Id;
        }

        public void Remove(int particleIndex)
        {
            throw new NotImplementedException();
        }

        public Span<FluidParticle> All()
        {
            var span = (Span<FluidParticle>)_particles;
            return span.Slice(0, _numParticles);
        }
        
        

        public void UpdateNeighbours()
        {
           _partitioningGrid.Clear();
           for (int i = 0; i < _numParticles; i++)
           {
               _partitioningGrid.Add(i, _particles[i].Position);
           }
        }


   

        public void Clear()
        {
            _numParticles = 0;
            _partitioningGrid.Clear();
 
        }
        
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion
        
    }
}
