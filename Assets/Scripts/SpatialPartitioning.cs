using System;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{

 

    public class SpatialPartitioning
    {
   
        private readonly float _neighbourhoodRadius;
        private readonly float _cellSize;
        private readonly Dictionary<int, List<(int, Vector2)>> _entities;
        private readonly Vector2[] _neighbourCellOffsets;
     
        public SpatialPartitioning(float neighbourhoodRadius)
        {
            _neighbourhoodRadius = neighbourhoodRadius;
            _cellSize = _neighbourhoodRadius;
            _entities = new Dictionary<int, List<(int, Vector2)>>();
            
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

        public void AddEntity(int id, Vector2 position)
        {
            GetCell(position).Add((id,position));
        }
    

        // TODO: remove position
        public void RemoveEntity(int id, Vector2 position)
        {
            GetCell(position).RemoveAll( x => x.Item1 == id);
        }
   
        public void MoveEntity(int id, Vector2 oldPosition, Vector2 newPosition)
        {
            int oldCellIndex = CellIndex(oldPosition);
            int newCellIndex = CellIndex(newPosition);
            
            if (newCellIndex == oldCellIndex) return;

            RemoveEntity(id, oldPosition);
            AddEntity(id, newPosition);
        }


        public Span<int> GetEntitiesInNeighbourhoodOf(Vector2 center)
        {
            List<int> result = new List<int>();

            foreach (var offset in _neighbourCellOffsets)
            {
                foreach ((int id, Vector2 position) in GetCell(center + offset))
                {
                    if ((position-center).magnitude <= _neighbourhoodRadius) result.Add(id);
                }
            }
 
            return result.ToArray();
        }


        public void RemoveAllEntities()
        {
           _entities.Clear();
        }
        
        public string DebugInfo()
        {
            string result = "";
            foreach (var cell in _entities)
            {
                result += "Cell " + cell.Key + " has " + cell.Value.Count + " entities\n";
            }
            result += "Total cells: " + _entities.Count;
            return result;
        }

        List<(int, Vector2)> GetCell(Vector2 position)
        {
            List<(int,Vector2)> result;
            if (_entities.TryGetValue(CellIndex(position), out result))
            {
                return result;
            }
            result = new List<(int, Vector2)>();
            _entities.Add(CellIndex(position), result);
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