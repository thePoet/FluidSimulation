using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FluidSimulation
{
    public class BlobBrush : MonoBehaviour
    {
        public int blobsPerFrame = 1;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;
        

        public GameObject liquidParticlePrefab;
        
        
        void Update()
        {
            Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);


            if (Input.GetMouseButton(0))
            {
                for (int i=0; i<blobsPerFrame; i++)
                {
                    SpawnSomething(liquidParticlePrefab, mousepos);
                }
            } 
            


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
            //liquidParticlePrefab.GetComponent<Rigidbody2D>().velocity = initialVelocity;
            
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
