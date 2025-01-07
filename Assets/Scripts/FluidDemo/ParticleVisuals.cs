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
        public GameObject heavygasParticlePrefab;
        public GameObject solidParticlePrefab;

        
        public GameObject Create(Particle particle)
        {
            return Create(particle.SubstanceId, particle.Position);
        }
        
        public void DestroyVisuals(Particle particle)
        {
            Destroy(particle.Visuals);
        }
        
        private GameObject Create(SubstanceId substanceId, Vector2 position)
        {
            var particle = Instantiate(PrefabFor(substanceId), parent: transform, worldPositionStays: false);
        
            particle.transform.position = new Vector3(position.x, position.y, 0f);

            return particle;
            
            GameObject PrefabFor(SubstanceId substance) => substance switch
            {
                SubstanceId.Water => liquidParticlePrefab,
                SubstanceId.Smoke => gasParticlePrefab,
                SubstanceId.HeavyGas => heavygasParticlePrefab,
                SubstanceId.Rock => solidParticlePrefab,
                SubstanceId.GreenLiquid => greenLiquidParticlePrefab,
                SubstanceId.RedLiquid => redLiquidParticlePrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(substance), substance, null)
            };
        }


    }
}