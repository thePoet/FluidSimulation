using UnityEngine;
using UnityEngine.Assertions;

namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// Represents a finite 2d grid. The squares of the grid  are represented by their
    /// x and y coordinates (0..Size.x and 0..Size.y) or their index (0..Size.x * Size.y - 1).
    /// </summary>
    public record Grid2D
    {
        /// <summary> The total number of squares in the grid. </summary>
        public int NumberOfSquares => Size.x * Size.y;

        /// <summary> The number of squares in the x and y axis of the grid. </summary>
        public Vector2Int Size { get; }

        /// <summary> The side length of the squares. </summary>
        public float SquareSize { get; }

        private readonly Vector2 _minCorner;
        private readonly Vector2 _maxCorner;

        public Grid2D(Vector2 origin, Vector2Int size, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell size must be greater than 0");
            Assert.IsTrue(size is { x: > 0, y: > 0 }, "Size in cells must be greater than 0");

            _minCorner = origin;
            _maxCorner = origin + new Vector2(size.x * squareSize, size.y * squareSize);
            Size = size;
            SquareSize = squareSize;
        }

        public Grid2D(Vector2 cornerMin, Vector2 cornerMax, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell size must be greater than 0");
            _minCorner = cornerMin;
            _maxCorner = cornerMax;
            SquareSize = squareSize;
            Size = SizeInSquares(cornerMin, cornerMax, squareSize);
        }

        public Grid2D(Rect rect, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell size must be greater than 0");
            _minCorner = rect.min;
            _maxCorner = rect.max;
            SquareSize = squareSize;
            Size = SizeInSquares(rect.min, rect.max, squareSize);
        }

        public Vector2Int SquareCoordinates(Vector2 position)
        {
            Vector2 relativePosition = position - _minCorner;
            return new Vector2Int(
                Mathf.FloorToInt(relativePosition.x / SquareSize),
                Mathf.FloorToInt(relativePosition.y / SquareSize)
            );
        }

        public Vector2Int SquareCoordinates(int cellIndex)
        {
            return new Vector2Int(cellIndex % Size.x, cellIndex / Size.x);
        }

        public bool IsInGrid(Vector2 position)
        {

            return position.x >= _minCorner.x && position.x <= _maxCorner.x &&
                   position.y >= _minCorner.y && position.y <= _maxCorner.y;
        }

        public bool IsValidSquare(Vector2Int cellCoordinates)
        {
            return cellCoordinates.x >= 0 && cellCoordinates.x < Size.x && cellCoordinates.y >= 0 &&
                   cellCoordinates.y < Size.y;
        }

        public bool IsValidSquareIndex(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < NumberOfSquares;
        }

        public int SquareIndex(Vector2 position)
        {
            Vector2Int cell = SquareCoordinates(position);
            return cell.x + cell.y * Size.x;
        }

        private static Vector2Int SizeInSquares(Vector2 cornerMin, Vector2 cornerMax, float cellSize)
        {
            Assert.IsTrue(cornerMin.x < cornerMax.x && cornerMin.y < cornerMax.y, "Invalid corners");

            return new Vector2Int(
                Mathf.CeilToInt((cornerMax.x - cornerMin.x) / cellSize),
                Mathf.CeilToInt((cornerMax.y - cornerMin.y) / cellSize)
            );
        }

    }
}
