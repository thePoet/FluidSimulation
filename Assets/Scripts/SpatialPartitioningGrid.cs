using System;
using FluidSimulation;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

public class SpatialPartitioningGrid<T>
{
    private readonly T[] _entities;
    private readonly int[] _numEntitiesInSquare;
    private readonly int _maxNumEntitiesInSquare;
    private readonly Grid2D _grid;
    
    public SpatialPartitioningGrid(Grid2D grid, int maxNumEntitiesInSquare)
    {
        _grid = grid;
        _maxNumEntitiesInSquare = maxNumEntitiesInSquare;
        _entities = new T[grid.NumberOfSquares * maxNumEntitiesInSquare];
        _numEntitiesInSquare = new int[grid.NumberOfSquares];

        Clear();
    }

    /// <summary>
    /// Return the indices of the entities in the same square as the given position
    /// </summary>
    /// <param name="offsetX">x offset for cell</param>
    /// <param name="offsetY">y offset for cell></param>
    public Span<T> SquareContents(Vector2 position)
    {
        if (!_grid.IsInGrid(position))
        {
            Debug.LogWarning("Position is not in the grid " + position );
            return Span<T>.Empty;
        }
        int cellIndex = _grid.SquareIndex(position);
        return new Span<T>(_entities, cellIndex * _maxNumEntitiesInSquare, _numEntitiesInSquare[cellIndex]);
    }

    public void Clear()
    {
        for (int i = 0; i < _numEntitiesInSquare.Length; i++)
        {
            _numEntitiesInSquare[i] = 0;
        }
    }

    public void Add(Span<T> entities, Func<T, Vector2> position)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            Add(entities[i], position(entities[i]));
        }
    }
  
    
    
    public void Add(T entity, Vector2 position, bool ignoreIfOutsideGrid=false)
    {
        if (!_grid.IsInGrid(position))
        {
            if (!ignoreIfOutsideGrid) Debug.LogWarning("Entity is outside the grid");
            return;
        }

        int cellIndex = _grid.SquareIndex(position);

        if (_numEntitiesInSquare[cellIndex] >= _maxNumEntitiesInSquare)
        {
            Debug.LogWarning("Too many entities in square");
            return;
        }

        int hash = cellIndex * _maxNumEntitiesInSquare + _numEntitiesInSquare[cellIndex];
        _entities[hash] = entity;
        _numEntitiesInSquare[cellIndex]++;
    }
    
   
}