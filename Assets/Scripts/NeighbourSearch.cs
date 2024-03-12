using System;
using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{
    public class NeighbourSearch
    {
        private readonly float _neighbourhoodRadius;
        private readonly float _cellSize;
        private readonly int _maxNumNeighbours;
        private readonly Dictionary<(int,int), List<(int, Vector2)>> _cells;
        private readonly Vector2[] _neighbourCellOffsets;
     
        private readonly Neighbours[] _neighbours;

        private struct Neighbours
        {
            public int[] Indices;
            public int NumNeighbours;
            public Span<int> IndicesSpan => new (Indices, 0, NumNeighbours);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public NeighbourSearch(float neighbourhoodRadius, int maxNumParticles, int maxNumNeighbours)
        {
            _neighbourhoodRadius = neighbourhoodRadius;
            _cellSize = _neighbourhoodRadius;
            _maxNumNeighbours = maxNumNeighbours;
            _cells = new Dictionary<(int,int), List<(int, Vector2)>>();
            
            _neighbours = new Neighbours[maxNumParticles];
            for (int i = 0; i < maxNumParticles; i++)
            {
                _neighbours[i] = new Neighbours
                {
                    Indices = new int[maxNumNeighbours],
                    NumNeighbours = 0
                };
            }
        }
        
        public Span<int> NeighboursOf(int particleIndex) => _neighbours[particleIndex].IndicesSpan;
        
        public void UpdateNeighbours(Span<FluidParticle> particles)
        {
            SpatialPartitioning(particles);
            PerformNeighbourSearch(particles);
        }
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private void SpatialPartitioning(Span<FluidParticle> particles)
        {
            foreach (var cell in _cells)
            {
                cell.Value.Clear();
            }

            for (int i = 0; i < particles.Length; i++)
            {
                var cellIndex = CellIndex(particles[i].Position);
                GetCell(cellIndex.x, cellIndex.y).Add((i, particles[i].Position));
            }
        }

        private void PerformNeighbourSearch(Span<FluidParticle> particles)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                int numNeighbours = FindNeighboursFor(i, particles[i].Position, _neighbours[i].Indices);
                _neighbours[i].NumNeighbours = numNeighbours;
            }
        }


        private int FindNeighboursFor(int particleIndex, Vector2 particlePosition, Span<int> result)
        {
            int p = 0;
            var cell = CellIndex(particlePosition);
     
            float neighRadiusSquared = _neighbourhoodRadius * _neighbourhoodRadius;
            
            for (int i=cell.x-1; i <= cell.x+1; i++)
            {
                for (int j=cell.y-1; j <= cell.y+1; j++)
                {
                    foreach ((int neighIndex, Vector2 position) in GetCell(i,j))
                    {
                        if ((position-particlePosition).sqrMagnitude <= neighRadiusSquared) 
                        {
                            if (neighIndex==particleIndex) continue; // Don't add self as neighbour
                            result[p] = neighIndex;
                            p++;
                            if (p == _maxNumNeighbours) return p;
                        }
                    }
                }
            }
      
            return p;
            
        }

     

        private List<(int, Vector2)> GetCell(int x, int y)
        {
            List<(int,Vector2)> result;
            
            if (_cells.TryGetValue((x,y), out result))
            {
                return result;
            }
            result = new List<(int, Vector2)>();
            _cells.Add((x,y), result);
            return result;
        }


        private (int x, int y) CellIndex(Vector2 position)
        {
            int x = Mathf.CeilToInt(position.x / _cellSize);
            int y = Mathf.CeilToInt(position.y / _cellSize);
            return (x, y);
        }
        #endregion
     
     
    }

}