using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


namespace FluidSimulation
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class LiquidParticle : MonoBehaviour
    {
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3(value.x, value.y, 0f);
        }

        public Color Color
        {
            get => _spriteRenderer.color;
            set => _spriteRenderer.color = value;
        }
        
        
        public Vector2 previousPosition;
        public List<LiquidParticle> neighbours;
        public Vector2 velocity;
        public float gravityMultiplier = 1f;
        public float movementMultiplier = 1f;
        
        
        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;
 
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Simulation.AddParticle(this);
        }

   
        private void ApplyForceToParticle(Vector2 force)
        {
            _rigidbody2D.AddForce(force);
        }

        #endregion
        
        
 

    }
}
