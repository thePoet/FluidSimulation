using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    public class BlobPhysics : MonoBehaviour, Blob.IPhysics
    {
        private Blob _blob;
        private Rigidbody2D _rigidbody2D;
        private void Awake()
        {
            _blob = GetComponent<Blob>();
            _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();

            CreateCollider();
        }

        public void SetVelocity(Vector2 velocity)
        {
            _rigidbody2D.velocity = velocity;
        }

        public void SetSize(float size)
        {
            GetComponent<CircleCollider2D>().radius = size;
        }

        private void CreateCollider()
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = _blob.radius;
        }
        

    }
}
