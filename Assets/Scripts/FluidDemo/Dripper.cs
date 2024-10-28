using UnityEngine;

namespace FluidDemo
{
    public class Dripper : MonoBehaviour
    {
        public bool onlyOne = false;

        private Simulation _fluidDemo;

        // Start is called before the first frame update
        void Start()
        {
            _fluidDemo = FindObjectOfType<Simulation>();
            if (onlyOne)
                SpawnParticle();
            else
                InvokeRepeating("SpawnParticle", 0.1f, 1f);
        }

        // Update is called once per frame
        void SpawnParticle()
        {
            _fluidDemo.SpawnParticle(transform.position, Vector2.zero, FluidId.Water);
        }
    }

}