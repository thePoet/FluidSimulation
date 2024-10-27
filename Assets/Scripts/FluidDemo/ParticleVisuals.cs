using System;
using System.Collections.Generic;
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

        private Dictionary<ParticleId, GameObject> _particles;
    
        private void Awake()
        {
            _particles = new Dictionary<ParticleId, GameObject>();
        }

        
        
        public void Add(ParticleId id, FluidId fluidId, Vector2 position)
        {
            if (_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " already exists in the visualization.");
                return;
            }

            var particle = Create(fluidId, position);
            particle.name = "Particle " + id.ToString();
            _particles.Add(id, particle);

        }

        public void Delete(ParticleId id)
        {
            if (!_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " does not exists in the visualization.");
                return;
            }

            var particle = _particles.GetValueOrDefault(id);
            Destroy(particle);

            _particles.Remove(id);
        }

        public void Clear()
        {
            foreach (var item in _particles)
            {
                Destroy(item.Value);
            }
            _particles.Clear();
        }

      
        public void UpdateParticle(ParticleId id, Vector2 position)
        {
            if (!_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " does not exists in the visualization.");
                return;
            }
            var particle = _particles.GetValueOrDefault(id);
            particle.transform.position = new Vector3(position.x, position.y, 0f);
        }

        public GameObject Create(FluidId fluidId, Vector2 position)
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