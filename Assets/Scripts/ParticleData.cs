using System;
using System.Collections.Generic;
using UnityEngine;

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

      
        private SpatialPartitioningGrid _partitioningGrid;
        private SpatialPartitioningGrid _neighbourGrid;

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
            
            var grid = new Grid2D(gridBounds, cellSize : _neighbourRadius);
            _partitioningGrid = new SpatialPartitioningGrid(grid,  maxNumParticlesInCell: 25);
            _neighbourGrid = new SpatialPartitioningGrid(grid,  maxNumParticlesInCell: 25*9);
            
            
            _neighbourIndices = new int[MaxNumberOfParticles * _maxNumNeighbours];
            _neighbourCount = new int[MaxNumberOfParticles];
           
           
        }

        public ComputeBuffer CreateParticlesBuffer() => new ComputeBuffer(MaxNumberOfParticles, FluidParticle.Stride);
        public void WriteParticlesToBuffer(ComputeBuffer buffer) => buffer.SetData(_particles);
        public void ReadParticlesFromBuffer(ComputeBuffer buffer) => buffer.GetData(_particles);
        
        public void ReadNeighboursFromBuffer(ComputeBuffer particleNeighbours, ComputeBuffer particleNeighbourCount)
        {
           particleNeighbours.GetData(_neighbourIndices);
           particleNeighbourCount.GetData(_neighbourCount);
        }
        
        // Temporary
        public void WriteNeighboursToBuffer(ComputeBuffer particleNeighbours, ComputeBuffer particleNeighbourCount)
        {

            for (int i = 0; i < _numParticles; i++)
            {
                _neighbourCount[i] = 0;
                
                
                foreach(int j in NeighbourIndices(i))
                {
                    if (j == i) continue;
                    if (Vector2.Distance(_particles[i].Position, _particles[j].Position) < _neighbourRadius)
                    {
                      
                        _neighbourIndices[i*_maxNumNeighbours + _neighbourCount[i]] = j;
                        _neighbourCount[i]++;
                        
                        
                        if (_neighbourCount[i] >= _maxNumNeighbours)
                        {Debug.Log("Max number of neighbours exceeded!");
                            break;
                        }
                    }
                }
            
            }
            particleNeighbours.SetData(_neighbourIndices);
            particleNeighbourCount.SetData(_neighbourCount);
            
            
        }
        
        public ComputeBuffer CreateSpatialBuffer() => new ComputeBuffer(MaxNumberOfParticles, FluidParticle.Stride);
        public void WriteSpatialToBuffer(ComputeBuffer buffer) => buffer.SetData(_particles);
        public void ReadSpatialFromBuffer(ComputeBuffer buffer) => buffer.GetData(_particles);


        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.PreviousPosition = particle.Position;
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

        public string Check()
        {
            string result = "";
            
            for (int i=0; i<_numParticles; i++)
            {
                if (float.IsNaN(_particles[i].Position.x)) result += "pos nan at: " + i + " ";
                if (float.IsNaN(_particles[i].Velocity.x)) result += "vel nan at: " + i + " ";
                if (float.IsNaN(_particles[i].Change.x)) result += "change nan at: " + i + " " + _particles[i].PreviousPosition;
                if (float.IsNaN(_particles[i].Pressure)) result += "pressure nan at: " + i + " ";
                if (float.IsNaN(_particles[i].NearPressure)) result += "near pressure nan at: " + i + " ";
               
            }

            return result;
        }

        public Span<int> NeighbourIndicesTest(int particleIndex)
        {
           
            var span = _neighbourIndices.AsSpan(particleIndex * _maxNumNeighbours, _neighbourCount[particleIndex]);

            /*
            string s = "";
            foreach (int i in span)
            {
              s+= i + " ";
            }
            Debug.Log(s);*/
            return span;
        }
        
        public Span<int> NeighbourIndices(int particleIndex)
        {

        
         //  return _neighbourSearch.NeighboursOf(particleIndex);
            return _neighbourGrid.GetParticlesInCell(_particles[particleIndex].Position);
        }
        
     
        

        public void UpdateNeighbours()
        {
           // _neighbourSearch.UpdateNeighbours(All());
           float t1 = Time.realtimeSinceStartup;
           _partitioningGrid.Clear();
           _partitioningGrid.AddParticles(All());
           float t2 = Time.realtimeSinceStartup;

           _neighbourGrid.Clear();
           _neighbourGrid.AddParticles(All(), addToNeighbourCellsAlso:true);
           float t3 = Time.realtimeSinceStartup;

//           Debug.Log("Partitioning grid: " + 1000f*(t2 - t1) + " Neighbour grid: " + 1000f*(t3 - t2) + " Total: " + 1000f*(t3 - t1) + " ms.");
        }


   

        public void Clear()
        {
            _numParticles = 0;
            _partitioningGrid.Clear();
            _neighbourGrid.Clear();
        }
        
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion
        
    }
}
