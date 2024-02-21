using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace FluidSimulation
{
    public enum StateOfMatter
    {
        Solid,
        Liquid,
        Gas
    }
    
    public class Blob : MonoBehaviour
    {
        public interface IPhysics
        {
            void SetVelocity(Vector2 velocity);
            void SetSize(float size);
        }

        public static List<Blob> blobs = new List<Blob>();
        
        public float radius;
 
        [Inject]
        public StateOfMatter state;
        
        public IPhysics physics;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rigidbody2D;

        [Inject]
        void Construct(System.Type phys) //TODO: pakko olla parempi tapa
        {
            if (!typeof(IPhysics).IsAssignableFrom(phys))
            {
                Debug.Log("Given type is not a physics type");
                return;
            }
            physics = gameObject.AddComponent(phys) as IPhysics;
        }
        void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            SetSpriteRadius(radius);
            blobs.Add(this);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public void SetSize(float size)
        {
            radius = size;
            SetSpriteRadius(radius);
            physics.SetSize(size);
        }
        public void SetColor(Color color)
        {
            _spriteRenderer.color = color;
        }
        
      
        public float Pressure()
        {
            return _rigidbody2D.totalForce.magnitude;
        }
        private void SetSpriteRadius(float spriteRadius)
        {
            _spriteRenderer.gameObject.transform.localScale = Vector3.one * spriteRadius;
        }
        #endregion
     
    }
}
