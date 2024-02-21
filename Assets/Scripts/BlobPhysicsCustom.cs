using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    public class BlobPhysicsCustom : MonoBehaviour, Blob.IPhysics
    {
        private Blob _blob;
        private Rigidbody2D _rigidbody2D;

        private static PhysicsMaterial2D _physicMaterial;
        
        
        private void Awake()
        {
            if (_physicMaterial == null)
            {
                _physicMaterial = CreatePhysicMaterial();
            }
            
            _blob = GetComponent<Blob>();
            _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            _rigidbody2D.sharedMaterial = _physicMaterial;
            gameObject.AddComponent<CircleCollider2D>();
            
            //gameObject.layer = 9;
        }

        private PhysicsMaterial2D CreatePhysicMaterial()
        {
            PhysicsMaterial2D physicMaterial = new PhysicsMaterial2D
            {
                bounciness = 0.0f,
                friction = 0.0f
            };
       

            return physicMaterial;
        }

      

        public void SetVelocity(Vector2 velocity)
        {
            _rigidbody2D.velocity = velocity;
        }

        public void SetSize(float size)
        {
            GetComponent<CircleCollider2D>().radius = size;
        }


        private void OnTriggerStay2D(Collider2D other)
        {
         
        }
    }
}
    
