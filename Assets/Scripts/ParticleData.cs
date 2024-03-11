using System;
using System.Linq;
using UnityEngine;

namespace FluidSimulation
{
    public class ParticleData : IParticleData
    {
        struct SpatialPartitioningCell
        {
            public int[] ParticleIndices;
            public int NumParticles;
        }
        
        
        private readonly int _maxNumParticles;
        private readonly int _maxNumNeighbours;
        private readonly int _maxNumParticlesInSpatialCell;
        private int _numParticles = 0;
        
        private readonly float _neighbourRadius;
        private readonly FluidParticle[] _particles;
        
        private readonly int[][] _neighbours;
        private readonly int[] _neighbourCount;
        
        private readonly (int, int)[] _particlePairs;
        private int _nextId = 0;
        private readonly SpatialPartitioning _partitioning;
        private readonly SpatialPartitioningCell[] _spatialPartitioningCells;
        private readonly Rect _bounds;
      //  private readonly Vector2[] _neighbourCellOffsets;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleData(int maxNumParticles, int maxNumNeighbours, float neighbourRadius, Rect bounds)
        {
            _maxNumParticles = maxNumParticles;
            _maxNumNeighbours = maxNumNeighbours;
            _neighbourRadius = neighbourRadius;
            _maxNumParticlesInSpatialCell = maxNumNeighbours * 2;
            _bounds = bounds;

            
            _particles = new FluidParticle[maxNumParticles];
            
            _neighbours = new int[maxNumParticles][];
            _neighbourCount = new int[maxNumParticles];
            for (int i = 0; i < maxNumParticles; i++)
            {
                _neighbours[i] = new int[maxNumNeighbours];
                _neighbourCount[i] = 0;
            }
       
            int maxNumParticlePairs = (int)(maxNumParticles * maxNumNeighbours * 0.5f) + 1;
            _particlePairs = new (int, int)[maxNumParticlePairs];
            
            
            _partitioning = new SpatialPartitioning(neighbourRadius, maxNumNeighbours);
            _spatialPartitioningCells = InitSpatialPartitioningCells(_bounds, _neighbourRadius);
/*
            _neighbourCellOffsets = new[]
            {
                Vector2.zero,
                new (_neighbourRadius, 0f),
                new (0f, _neighbourRadius),
                new (_neighbourRadius, _neighbourRadius),
                new (-_neighbourRadius, 0f),
                new (0f, -_neighbourRadius),
                new (-_neighbourRadius, -_neighbourRadius),
                new (-_neighbourRadius, _neighbourRadius),
                new (_neighbourRadius, -_neighbourRadius)
            };*/
            
            SpatialPartitioningCell[] InitSpatialPartitioningCells(Rect bounds, float cellSize)
            {
                var result = new SpatialPartitioningCell[NumSpatialPartitioningCells(bounds, cellSize)];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new SpatialPartitioningCell
                    {
                        ParticleIndices = new int[_maxNumParticlesInSpatialCell],
                        NumParticles = 0
                    };
                }

                return result;
            }
        }

        

        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.PreviousPosition = particle.Position;
            
            particle.Id = _nextId;
            _nextId++;
            
            _numParticles++;
            int index = _numParticles - 1;
            
            _particles[index] = particle;
            _neighbourCount[index] = 0;
            
            _partitioning.AddEntity(index, particle.Position);
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

        public Span<int> NeighbourIndices(int particleIndex)
        {
            var span =  (Span<int>)_neighbours[particleIndex];
            return span.Slice(0, _neighbourCount[particleIndex]);
        }

        public Span<(int, int)> NeighbourParticlePairs()
        {
            int p = 0;
            
            Span<(int, int)> pairs = _particlePairs;
            
            for (int indexA = 0; indexA < _numParticles; indexA++)
            {
                for (int i = 0; i < _neighbourCount[indexA]; i++)
                {
                    int indexB = _neighbours[indexA][i];
                    if (_particles[indexA].Id > _particles[indexB].Id)
                    {
                        pairs[p] = (indexA, indexB);
                        p++;
                    }
                }
            }

            return pairs.Slice(0, p);
        }
       
        public void UpdateNeighbours()
        {
            _partitioning.UpdateNeighbours(All(), _neighbours, _neighbourCount);
            /*
            for (int i = 0; i < _numParticles; i++)
                _partitioning.UpdateEntity(i, _particles[i].Position);
            
            for (int i = 0; i < _numParticles; i++)
            {
                int numNeighbours = _partitioning.FindNeighboursFor(_neighbours[i], _particles[i].Position);
                _neighbourCount[i] = numNeighbours;
            }
            */
        
        }

        

        public void Clear()
        {
            _numParticles = 0;
            _partitioning.Clear();
        }
        
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

     
        private int NumSpatialPartitioningCells(Rect bounds, float cellSize)
        {
            int x = Mathf.CeilToInt(bounds.width / cellSize);
            int y = Mathf.CeilToInt(bounds.height / cellSize);
            return x * y;
        }

        private int SpatialPartitioningCellIndex(Vector2 position, Rect bounds, float cellSize)
        {
            if (!bounds.Contains(position))
            {
                position = new Vector2( Mathf.Clamp(position.x, bounds.xMin, bounds.xMax*0.999999f),
                                        Mathf.Clamp(position.y, bounds.yMin, bounds.yMax*0.999999f) );
            }
            Vector2 relPosition = position - bounds.min;
            int index =  Mathf.FloorToInt(relPosition.x / cellSize)
                   + Mathf.FloorToInt(relPosition.y / cellSize) 
                   * Mathf.CeilToInt(bounds.width / cellSize);
            
            
            if (index < 0 || index >= NumSpatialPartitioningCells(bounds, cellSize))
            {
                Debug.LogError("Index out of bounds: " + index);
                Debug.LogError("Bounds " + bounds);
                Debug.LogError("Position " + position);
            }
            
            return index;
            
        }

        #endregion

        
    }
}
