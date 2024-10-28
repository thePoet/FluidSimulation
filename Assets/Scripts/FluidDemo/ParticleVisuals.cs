using System;
using UnityEngine;


namespace FluidDemo
{
    public class ParticleVisuals : MonoBehaviour
    {
        public GameObject liquidParticlePrefab;
        public GameObject greenLiquidParticlePrefab;
        public GameObject redLiquidParticlePrefab;
        public GameObject gasParticlePrefab;
        public GameObject solidParticlePrefab;

        
        public GameObject Create(Particle particle)
        {
            return Create(particle.FluidId, particle.Position);
        }
        
        public void DestroyVisuals(Particle particle)
        {
            Destroy(particle.Visuals);
        }
        
        private GameObject Create(FluidId fluidId, Vector2 position)
        {
            var particle = Instantiate(PrefabFor(fluidId), parent: transform, worldPositionStays: false);
        
            particle.transform.position = new Vector3(position.x, position.y, 0f);

            return particle;
            
            GameObject PrefabFor(FluidId substance) => substance switch
            {
                FluidId.Water => liquidParticlePrefab,
                FluidId.Smoke => gasParticlePrefab,
                FluidId.Rock => solidParticlePrefab,
                FluidId.GreenLiquid => greenLiquidParticlePrefab,
                FluidId.RedLiquid => redLiquidParticlePrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(substance), substance, null)
            };
        }


    }
}