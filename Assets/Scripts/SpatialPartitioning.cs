using System;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{

 

    public class SpatialPartitioning<T> where T : IPosition
    {
   
        private readonly float _neighbourhoodRadius;
        private readonly float _cellSize;
        private readonly Dictionary<int, List<T>> _cells;
        private readonly Vector2[] _neighbourCellOffsets;
     
        public SpatialPartitioning(float neighbourhoodRadius)
        {
            _neighbourhoodRadius = neighbourhoodRadius;
            _cellSize = _neighbourhoodRadius;
            _cells = new Dictionary<int, List<T>>();
            
            _neighbourCellOffsets = new[]
            {
                Vector2.zero,
                new (_cellSize, 0f),
                new (0f, _cellSize),
                new (_cellSize, _cellSize),
                new (-_cellSize, 0f),
                new (0f, -_cellSize),
                new (-_cellSize, -_cellSize),
                new (-_cellSize, _cellSize),
                new (_cellSize, -_cellSize)
            };
        }

        public void AddEntity(T entity, Vector2 position)
        {
            GetCell(position).Add(entity);
        }

        public void RemoveEntity(T entity, Vector2 position)
        {
            GetCell(position).Remove(entity);
        }

        public void MoveEntity(T entity, Vector2 oldPosition, Vector2 newPosition)
        {
            int oldCellIndex = CellIndex(oldPosition);
            int newCellIndex = CellIndex(newPosition);
            
            if (newCellIndex == oldCellIndex) return;

            GetCell(oldPosition).Remove(entity);
            GetCell(newPosition).Add(entity);
        }


        public List<T> GetEntiesInNeighbourhoodOf(Vector2 center)
        {
            List<T> result = new List<T>();

            foreach (var offset in _neighbourCellOffsets)
            {
                foreach (var entity in GetCell(center + offset))
                {
                    if ((entity.Position-center).magnitude <= _neighbourhoodRadius) result.Add(entity);
                }
            }
 
            return result;
        }


        public void RemoveAllEntities()
        {
            foreach (var cell in _cells)
            {
                cell.Value.Clear();
            }
        }
        
        public string DebugInfo()
        {
            string result = "";
            foreach (var cell in _cells)
            {
                result += "Cell " + cell.Key + " has " + cell.Value.Count + " entities\n";
            }
            result += "Total cells: " + _cells.Count;
            return result;
        }

        List<T> GetCell(Vector2 position)
        {
            List <T> result;
            if (_cells.TryGetValue(CellIndex(position), out result))
            {
                return result;
            }
            result = new List<T>();
            _cells.Add(CellIndex(position), result);
            return result;
        }
        
     
        int CellIndex(Vector2 position)
        {
            int x = Mathf.CeilToInt(position.x / _cellSize);
            int y = Mathf.CeilToInt(position.y / _cellSize);
            return CellIndex(x, y);
        }
        
        int CellIndex(int x, int y)
        {
            return x * 100000 + y;
        }
        
  


    }

}