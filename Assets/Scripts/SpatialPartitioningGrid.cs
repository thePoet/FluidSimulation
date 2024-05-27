using System;
using FluidSimulation;
using UnityEngine;

public class SpatialPartitioningGrid
{
    private readonly int[] _particleIndices;
    private readonly int[] _numParticlesInCell;
    readonly int _maxNumParticlesInCell;
    private readonly Grid2D _grid;
    
    public SpatialPartitioningGrid(Grid2D grid, int maxNumParticlesInCell)
    {
        _grid = grid;
        _maxNumParticlesInCell = maxNumParticlesInCell;
        _particleIndices = new int[grid.NumberOfCells * maxNumParticlesInCell];
        _numParticlesInCell = new int[grid.NumberOfCells];

        Clear();
    }

    /// <summary>
    /// Return the indices of the particles in the same cell as the given position
    /// </summary>
    /// <param name="offsetX">x offset for cell</param>
    /// <param name="offsetY">y offset for cell></param>
    public Span<int> GetParticlesInCell(Vector2 position)
    {
        if (!_grid.IsInGrid(position)) return Span<int>.Empty;
        int cellIndex = _grid.CellIndex(position);
        return new Span<int>(_particleIndices, cellIndex * _maxNumParticlesInCell, _numParticlesInCell[cellIndex]);
    }



   

    public void Clear()
    {
        for (int i = 0; i < _numParticlesInCell.Length; i++)
        {
            _numParticlesInCell[i] = 0;
        }
    }

    public void AddParticles(Span<FluidParticle> particles, bool addToNeighbourCellsAlso = false)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            if (!addToNeighbourCellsAlso)
            {
                AddParticle(i, particles[i].Position);
            }
            else
            {
                for (int x=-1; x<=1; x++)
                {
                    for (int y=-1; y<=1; y++)
                    {
                        Vector2 adjustedPosition = particles[i].Position + new Vector2(x, y) * _grid.CellSize;
                        AddParticle(i, adjustedPosition, ignoreIfOutsideGrid:true);
                    }
                }
            }
        }
        
        void AddParticle(int index, Vector2 position, bool ignoreIfOutsideGrid=false)
        {
            if (!_grid.IsInGrid(position))
            {
                if (!ignoreIfOutsideGrid) Debug.LogWarning("Particle is not in the grid");
                return;
            }

            int cellIndex = _grid.CellIndex(position);

            if (_numParticlesInCell[cellIndex] >= _maxNumParticlesInCell)
            {
                Debug.LogWarning("Too many particles in cell");
                return;
            }

            int hash = cellIndex * _maxNumParticlesInCell + _numParticlesInCell[cellIndex];
            _particleIndices[hash] = index;
            _numParticlesInCell[cellIndex]++;
        }
        
    }
    
   
}