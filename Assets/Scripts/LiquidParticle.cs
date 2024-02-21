using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class LiquidParticle : MonoBehaviour
    {
   
  

       // public static SimulationSettings settings = new SimulationSettings();
        
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3(value.x, value.y, 0f);
        }

        public Vector2 newPosition;
        public Vector2 startPosition;
        
        public float ScalingFactor;
        public List<LiquidParticle> neighbours;
        
        

        public Vector2 velocity;
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
