using System;
using System.Collections;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Elemental
{
    public class BlobBrush : MonoBehaviour
    {
        public GameObject[] blobs;
        public int blobsPerFrame = 1;
        public float brushRadius = 10f;
        public float sizeMin = 0.5f;
        public float sizeMax = 20f;
        public float maxSpeed = 10f;
        
        
        [Inject]
        private BlobFactory _blobFactory;

        public GameObject something;
        
        
        void Update()
        {
            Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

/*
            if (Input.GetMouseButton(1))
            {
                for (int i=0; i<blobsPerFrame; i++)
                {
                    SpawnBlob(blobs[0], mousepos);
                }
            } */
            if (Input.GetMouseButtonDown(0))
            {
                for (int i=0; i<blobsPerFrame; i++)
                {
                    SpawnSomething(something, mousepos);
                }
            } 
            
            if (Input.GetKey(KeyCode.N)) Debug.Log("Number of blobs: " + Blob.blobs.Count);

        }

       

        private static void LogMaxPressure()
        {
            if (Blob.blobs.Count > 10)
            {
                float max = 0f;
                foreach (var blob in Blob.blobs)
                {
                    if (blob.Pressure() > max) max = blob.Pressure();
                }

                Debug.Log(max);
            }
        }

        private void SpawnBlob(GameObject prefab, Vector3 mousepos)
        {
            Vector3 randomOffset = Random.insideUnitCircle * brushRadius;
            Vector3 spawnPos = mousepos + randomOffset;
            spawnPos = spawnPos.SetZ(0f);

            float radius = Random.Range(sizeMin, sizeMax);
            Vector2 initialVelocity = randomOffset.normalized * maxSpeed;
            initialVelocity += Random.insideUnitCircle * maxSpeed * 0.2f;

            var blob = _blobFactory.Create(StateOfMatter.Liquid);
            blob.SetSize(radius);
            blob.physics.SetVelocity(initialVelocity);
            blob.transform.position = spawnPos;
            blob.SetColor(RandomColor());
    
        }
        
        private void SpawnSomething(GameObject prefab, Vector3 mousepos)
        {
            Vector3 randomOffset = Random.insideUnitCircle * brushRadius;
            Vector3 spawnPos = mousepos + randomOffset;
            spawnPos = spawnPos.SetZ(0f);

        //    float radius = Random.Range(sizeMin, sizeMax);
            Vector2 initialVelocity = randomOffset.normalized * maxSpeed;
            initialVelocity += Random.insideUnitCircle * maxSpeed * 0.2f;

            var something = Instantiate(prefab, spawnPos, Quaternion.identity);
            //something.GetComponent<Rigidbody2D>().velocity = initialVelocity;
            
            //var blob = _blobFactory.Create(StateOfMatter.Liquid);
          //  blob.SetSize(radius);
            //blob.physics.SetVelocity(initialVelocity);
            //blob.transform.position = spawnPos;
            //blob.SetColor(RandomColor());
    
        }
        
        
        Color RandomColor()
        {
            return new Color(Random.value, Random.value, Random.value);
        }
        
    }
}
