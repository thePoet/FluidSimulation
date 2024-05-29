using System;
using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{
    public class ParticleVisualization : MonoBehaviour
    {
        public GameObject liquidParticlePrefab;
        public GameObject solidParticlePrefab;

        private Dictionary<int, GameObject> _particles;

        private void Awake()
        {
            _particles = new Dictionary<int, GameObject>();
        }

        public void AddParticle(int id, ParticleType particleType)
        {
            if (_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " already exists in the visualization.");
                return;
            }

            var particle = Instantiate(PrefabFor(particleType), parent: transform, worldPositionStays: false);
            _particles.Add(id, particle);


            GameObject PrefabFor(ParticleType pType) => pType switch
            {
                ParticleType.Liquid => liquidParticlePrefab,
                ParticleType.Solid => solidParticlePrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(particleType), particleType, null)
            };

        }

        public void RemoveParticle(int id)
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

        public void UpdateParticle(int id, Vector2 position)
        {
            if (!_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " does not exists in the visualization.");
                return;
            }

            var particle = _particles.GetValueOrDefault(id);
            particle.transform.position = new Vector3(position.x, position.y, 0f);
        }

        public void ColorParticle(int id, Color color)
            => _particles[id].GetComponentInChildren<SpriteRenderer>().color = color;
        
        
        public Color RandomColor => new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }
}