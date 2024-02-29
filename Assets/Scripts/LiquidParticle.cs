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

        public Vector2 previousPosition;
        
        public float ScalingFactor;
        public List<LiquidParticle> neighbours;
        public Vector2 velocity;

        public Vector2 debug1 = Vector2.zero;
        public Vector2 debug2 = Vector2.zero;

        private Rigidbody2D _rigidbody2D;
        
 
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            Simulation.AddParticle(this);
        }

   
        private void ApplyForceToParticle(Vector2 force)
        {
            _rigidbody2D.AddForce(force);
        }

        #endregion
        
        
 

    }
}
