using System;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{
    public class SpatialPartitioningGrid<T>
    {
        private readonly T[] _entities;
        private readonly int[] _numEntitiesInSquare;
        private readonly int _maxNumEntitiesInSquare;
        private readonly Grid2D _grid;

        private readonly Func<T, Vector2> _positionForEntity;
    
        public SpatialPartitioningGrid(Grid2D grid, int maxNumEntitiesInSquare, Func<T, Vector2> positionForEntity)
        {
            _grid = grid;
            _maxNumEntitiesInSquare = maxNumEntitiesInSquare;
            _entities = new T[grid.NumberOfSquares * maxNumEntitiesInSquare];
            _numEntitiesInSquare = new int[grid.NumberOfSquares];
            _positionForEntity = positionForEntity;

            Clear();
        }

        /// <summary>
        /// Return the indices of the entities in the same partitioning square as the given position
        /// </summary>
        ///
    
        /*public Span<T> GridSquareContents(Vector2 position)
    {
        if (!_grid.IsInGrid(position))
        {
            Debug.LogWarning("Position is not in the grid " + position );
            return Span<T>.Empty;
        }
        int squareIndex = _grid.SquareIndex(position);
        return new Span<T>(_entities, squareIndex * _maxNumEntitiesInSquare, _numEntitiesInSquare[squareIndex]);
    }*/


        public void Clear()
        {
            for (int i = 0; i < _numEntitiesInSquare.Length; i++)
            {
                _numEntitiesInSquare[i] = 0;
            }
        }



        /// <summary>
        ///  returns entities inside the given rectangle
        /// </summary>
        public T[] RectangleContents(Rect rect)
        {
            List<T> result = new List<T>();
        
            foreach (var squareIdx in _grid.SquareIndicesInRect(rect))
            {
                foreach (T entity in GridSquareContents(squareIdx))
                {
                    result.Add(entity);
                }
            }

            return result.ToArray();
        }
    
        /// <summary>
        /// Return entities within given radius of a position.
        /// </summary>
        public T[] CircleContents(Vector2 position, float radius)
        {
            var minCorner = position - Vector2.one * radius;
            var maxCorner = position + Vector2.one * radius;
            Rect rect = new Rect(minCorner, maxCorner - minCorner);

            List<T> result = new List<T>();
        
            foreach (T entity in RectangleContents(rect))
            {
                if (Vector2.Distance(position, Position(entity)) <= radius)
                {
                    result.Add(entity);
                }
            }
        
            return result.ToArray();
        }


        public void Add(Span<T> entities)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Add(entities[i]);
            }
        }
  
    
    
        public void Add(T entity, bool ignoreIfOutsideGrid=false)
        {
            if (!_grid.IsInGrid(Position(entity)))
            {
                if (!ignoreIfOutsideGrid) Debug.LogWarning("Entity is outside the grid");
                return;
            }

            int cellIndex = _grid.SquareIndex(Position(entity));

            if (_numEntitiesInSquare[cellIndex] >= _maxNumEntitiesInSquare)
            {
                Debug.LogWarning("Too many entities in square");
                return;
            }

            int hash = cellIndex * _maxNumEntitiesInSquare + _numEntitiesInSquare[cellIndex];
            _entities[hash] = entity;
 
            _numEntitiesInSquare[cellIndex]++;
        }
    
        public void FillFromRawData(T[] entities, int[] numEntitiesInSquare)
        {
            if (entities.Length != _entities.Length || numEntitiesInSquare.Length != _numEntitiesInSquare.Length)
            {
                Debug.LogWarning("Array sizes do not match");
                return;
            }
        
            Array.Copy(entities, _entities, _entities.Length);
            Array.Copy(numEntitiesInSquare, _numEntitiesInSquare, _numEntitiesInSquare.Length);
        }

        
        public Span<T> GridSquareContents(int squareIndex)
        {
            return new Span<T>(_entities, squareIndex * _maxNumEntitiesInSquare, _numEntitiesInSquare[squareIndex]);
        }

        public int NumSquares => _grid.NumberOfSquares;
    
        private Vector2 Position(T entity) => _positionForEntity(entity);
    
   
    }
}