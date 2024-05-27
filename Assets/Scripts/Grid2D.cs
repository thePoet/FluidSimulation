using UnityEngine;
using UnityEngine.Assertions;


/// <summary>
/// Represents a finite 2d grid for spatial partitioning. The square cells of the grid  are represented by their
/// x and y coordinates (0..Size.x and 0..Size.y) or their cell index (0..Size.x * Size.y - 1).
/// </summary>
public record Grid2D
{
    /// <summary> The total number of cells in the grid. </summary>
    public int NumberOfCells => Size.x * Size.y;
    /// <summary> The number of squares in the x and y axis of the grid. </summary>
    public Vector2Int Size { get; }
    
    /// <summary> The side length of the squares. </summary>
    public float CellSize { get; }

    private readonly Vector2 _minCorner;
    private readonly Vector2 _maxCorner;
    
    public Grid2D(Vector2 origin, Vector2Int size, float cellSize)
    {
        Assert.IsTrue(cellSize > 0, "Cell size must be greater than 0");
        Assert.IsTrue(size is { x: > 0, y: > 0 }, "Size in cells must be greater than 0");
        _minCorner = origin;
        _maxCorner = origin + new Vector2(size.x * cellSize, size.y * cellSize);
        Size = size;
        CellSize = cellSize;
    }

    public Grid2D(Vector2 cornerMin, Vector2 cornerMax, float cellSize)
    {
        Assert.IsTrue(cellSize > 0, "Cell size must be greater than 0");
        _minCorner = cornerMin;
        _maxCorner = cornerMax;
        CellSize = cellSize;
        Size = SizeInCells(cornerMin, cornerMax, cellSize);
    }
    
    public Grid2D(Rect rect, float cellSize)
    {
        Assert.IsTrue(cellSize > 0, "Cell size must be greater than 0");
        _minCorner = rect.min;
        _maxCorner = rect.max;
        CellSize = cellSize;
        Size = SizeInCells(rect.min, rect.max, cellSize);
    }
    
    public Vector2Int CellCoordinates(Vector2 position)
    {
        Vector2 relativePosition = position - _minCorner;
        return new Vector2Int(
            Mathf.FloorToInt(relativePosition.x / CellSize),
            Mathf.FloorToInt(relativePosition.y / CellSize)
        );
    }
    
    public Vector2Int CellCoordinates(int cellIndex)
    {
        return new Vector2Int(cellIndex % Size.x, cellIndex / Size.x);
    }
    
    public bool IsInGrid(Vector2 position)
    {

        return position.x >= _minCorner.x && position.x <= _maxCorner.x &&
               position.y >= _minCorner.y && position.y <= _maxCorner.y;
    }
    
    public bool IsValidCell(Vector2Int cellCoordinates)
    {
        return cellCoordinates.x >= 0 && cellCoordinates.x < Size.x && cellCoordinates.y >= 0 && cellCoordinates.y < Size.y;
    }
    
    public bool IsValidCellIndex(int cellIndex)
    {
        return cellIndex >= 0 && cellIndex < NumberOfCells;
    }
    
    public int CellIndex(Vector2 position)
    {
        Vector2Int cell = CellCoordinates(position);
        return cell.x + cell.y * Size.x;
    }
    
    private static Vector2Int SizeInCells(Vector2 cornerMin, Vector2 cornerMax, float cellSize)
    {
        Assert.IsTrue(cornerMin.x < cornerMax.x && cornerMin.y < cornerMax.y, "Invalid corners");

        return new Vector2Int(
            Mathf.CeilToInt((cornerMax.x - cornerMin.x) / cellSize),
            Mathf.CeilToInt((cornerMax.y - cornerMin.y) / cellSize)
        );
    }
    
}
