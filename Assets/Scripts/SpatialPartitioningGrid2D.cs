using System;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{

    public class OutOfPartitioningGridBounds : Exception
    {
    }

    public class SpatialPartitioningGrid2D<T> : ISpatialPartitioning2D<T>
    {
        private readonly Rect _bounds;
        private readonly float _cellSize;
        private readonly int _numCellsX;
        private readonly int _numCellsY;
        private readonly List<T>[,] _cells;

        public SpatialPartitioningGrid2D(Rect bounds, float cellSize)
        {
            _bounds = bounds;
            _cellSize = cellSize;
            _numCellsX = (int)(_bounds.width / _cellSize) + 1;
            _numCellsY = (int)(_bounds.height / _cellSize) + 1;
            _cells = new List<T>[_numCellsX, _numCellsY];

            for (int x = 0; x < _numCellsX; x++)
            {
                for (int y = 0; y < _numCellsY; y++)
                {
                    _cells[x, y] = new List<T>();
                }
            }
        }





        public void AddEntity(T entity, Vector2 position)
        {
            if (!_bounds.Contains(position)) throw new OutOfPartitioningGridBounds();
            GetCell(position).Add(entity);
        }

        public void RemoveEntity(T entity, Vector2 position)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateEntity(T entity, Vector2 oldPosition, Vector2 newPosition)
        {
            if (!_bounds.Contains(newPosition)) throw new OutOfPartitioningGridBounds();
                
                
            List<T> oldCell = GetCell(oldPosition);
            List<T> newCell = GetCell(newPosition);

            if (oldCell != newCell)
            {
                oldCell.Remove(entity);
                newCell.Add(entity);
            }
        }

        public List<T> GetEntitiesInsideCircle(Vector2 center, float radius)
        {
            Vector2 min = center - new Vector2(radius, radius);
            Vector2 max = center + new Vector2(radius, radius);

            min = min.Clamp(_bounds);
            max = max.Clamp(_bounds);

            List<T> result = new List<T>();
            
            
            for (int x = CellXIndex(min); x <= CellXIndex(max); x++)
            {
                for (int y = CellYIndex(min); y <= CellYIndex(max); y++)
                {
                    // HUOM ETAISYYTTA EI TARKISTETA
                    result.AddRange(_cells[x, y]);
                }
            }

            return result;
        }

        public void RemoveAllEntities()
        {
            foreach (var cell in _cells)
            {
                cell.Clear();
            }
        }

        List<T> GetCell(Vector2 position)
        {
            int x = CellXIndex(position);
            int y = CellYIndex(position);
            return _cells[x, y];
        }
        
        int CellXIndex(Vector2 position)
        {
            return (int)((position.x - _bounds.x) / _cellSize);
        }
        
        int CellYIndex(Vector2 position)
        {
            return (int)((position.y - _bounds.y) / _cellSize);
        }


    }

}
