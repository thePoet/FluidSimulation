using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;




namespace FluidSimulation
{
    public interface IPosition
    {
        Vector2 Position { get;  }
    }
    
   
    public class LiquidParticle : MonoBehaviour, IPosition
    {
        static public SpatialPartitioning<LiquidParticle> partitioning;

        static LiquidParticle()
        {
            partitioning = new SpatialPartitioning<LiquidParticle>(15f);
        }
        
            
        public Vector2 Position
        {
            get => transform.position;
            set
            {
                partitioning.MoveEntity(this, transform.position, value);
                transform.position = new Vector3(value.x, value.y, 0f);
            }
        }

        public Color Color
        {
            get => _spriteRenderer.color;
            set => _spriteRenderer.color = value;
        }
        
        public void UpdateNeighbours()
        {
            neighbours = partitioning.GetEntiesInNeighbourhoodOf(Position);
        }
        
        public Vector2 previousPosition;
        public List<LiquidParticle> neighbours;
        public Vector2 velocity;
        public float gravityMultiplier = 1f;
        public float movementMultiplier = 1f;
        
        
   
        private SpriteRenderer _spriteRenderer;

        
        
 
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------

        private void Awake()
        {
        
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            partitioning.AddEntity(this, Position);
            Simulation.AddParticle(this);
        }

   

        #endregion
        
        
 

    }
}
