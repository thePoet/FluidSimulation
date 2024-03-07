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
        public readonly Dictionary<int, List<(int, Vector2)>> _cells;
        private readonly Dictionary<int, int> _entityToCell;
        private readonly Vector2[] _neighbourCellOffsets;
     
        public SpatialPartitioning(float neighbourhoodRadius)
        {
            _neighbourhoodRadius = neighbourhoodRadius;
            _cellSize = _neighbourhoodRadius;
            _cells = new Dictionary<int, List<(int, Vector2)>>();
            _entityToCell = new Dictionary<int, int>();
            
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
            int cellIndex = CellIndex(position);
            _entityToCell.Add(id, cellIndex);
            
            GetCell(cellIndex).Add((id,position));
        }
    

        // TODO: remove position
        public void RemoveEntity(int id, Vector2 position)
        {
            throw new NotImplementedException();
            //   GetCell(position).RemoveAll( x => x.Item1 == id);
        }
   
        public void UpdateEntity(int id, Vector2 newPosition)
        {
            int oldCellIndex = _entityToCell[id];
            int newCellIndex = CellIndex(newPosition);
            
           // if (newCellIndex == oldCellIndex) return;

            _cells[oldCellIndex].RemoveAll(x=>x.Item1 == id);
            GetCell(newCellIndex).Add((id, newPosition));
            _entityToCell[id] = newCellIndex;
        }


        public Span<int> GetEntitiesInNeighbourhoodOf(Vector2 center)
        {
            List<int> result = new List<int>();

            foreach (var offset in _neighbourCellOffsets)
            {
                foreach ((int id, Vector2 position) in GetCell(CellIndex(center + offset)))
                {
                    if ((position-center).magnitude <= _neighbourhoodRadius) 
                        result.Add(id);
                }
            }
 
            return result.ToArray();
        }


        public void RemoveAllEntities()
        {
           _cells.Clear();
        }
        
        
        
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
        }

        public List<(int, Vector2)> GetCell(int index)
        {
            List<(int,Vector2)> result;
            
            if (_cells.TryGetValue(index, out result))
            {
                return result;
            }
            result = new List<(int, Vector2)>();
            _cells.Add(index, result);
            return result;
        }
        
        
     
        public int CellIndex(Vector2 position)
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