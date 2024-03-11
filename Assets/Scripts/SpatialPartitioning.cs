using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{

 

    public class SpatialPartitioning
    {
        private readonly float _neighbourhoodRadius;
        private readonly float _cellSize;
        private readonly int _maxNumNeighbours;
        private readonly Dictionary<(int,int), List<(int, Vector2)>> _cells;
        private readonly Vector2[] _neighbourCellOffsets;
     
        public SpatialPartitioning(float neighbourhoodRadius, int maxNumNeighbours)
        {
            _neighbourhoodRadius = neighbourhoodRadius;
            _cellSize = _neighbourhoodRadius;
            _maxNumNeighbours = maxNumNeighbours;
            _cells = new Dictionary<(int,int), List<(int, Vector2)>>();
        }

        public void AddEntity(int id, Vector2 position)
        {
            var cellIndex = CellIndex(position);
            GetCell(cellIndex.x, cellIndex.y).Add((id,position));
        }
        
        public void UpdateNeighbours(Span<FluidParticle> particles, int[][] neighbours, int[] neighbourCount)
        {
            ClearSpatialPartitioning();
            DoSpatialPartitioning(particles);
            FindNeighboursForParticles(particles);

            void ClearSpatialPartitioning()
            {
                foreach (var cell in _cells)
                {
                    cell.Value.Clear();
                }
            }

            void DoSpatialPartitioning(Span<FluidParticle> particles)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    var cellIndex = CellIndex(particles[i].Position);
                    GetCell(cellIndex.x, cellIndex.y).Add((i, particles[i].Position));
                }
            }
            void FindNeighboursForParticles(Span<FluidParticle> particles)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    int numNeighbours = FindNeighboursFor(i, particles[i].Position, neighbours[i]);
                    neighbourCount[i] = numNeighbours;
                }
            }
            
        }


        
        public int FindNeighboursFor(int particleIndex, Vector2 particlePosition, Span<int> result)
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

      
        public void Clear()
        {
            _cells.Clear();
        }
        /*
        public string DebugInfo()
        {
            string result = "";
            int numParticles = 0;
            foreach (var cell in _cells)
            {
                result += "Cell " + cell.Key + " has " + cell.Value.Count + " entities\n";
                numParticles+= cell.Value.Count;
            }
            result += "Total cells: " + _cells.Count;
            result += "Total particles: " + numParticles;
            result += "Cell size: " + _cellSize;
            return result;
        }*/

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
       

        // uutta roinaa
        int NumSpatialPartitioningCells(Rect bounds, float cellSize)
        {
            int x = Mathf.CeilToInt(bounds.width / cellSize);
            int y = Mathf.CeilToInt(bounds.height / cellSize);
            return x * y;
        }

        int SpatialPartitioningCellIndex(Vector2 position, Rect bounds, float cellSize)
        {
            if (!bounds.Contains(position))
            {
                position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
                position.y = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax);
            }
            Vector2 relPosition = position - bounds.min;
            return (int)(relPosition.x / cellSize)
                        + (int)(relPosition.y / cellSize) 
                        * Mathf.CeilToInt(bounds.width / cellSize);
            
        }
        
     
     
    }

}