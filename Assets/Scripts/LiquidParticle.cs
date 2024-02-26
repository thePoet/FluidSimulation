using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{

    public class LiquidParticle : MonoBehaviour
    {
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3(value.x, value.y, 0f);
        }

        public Vector2 startPosition;
        
        public float ScalingFactor;
        public List<LiquidParticle> neighbours;
        public Vector2 velocity;
        
    
        
 
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------

        private void Awake()
        {
        
        }

   
      

        #endregion
        
        
 

    }
}
