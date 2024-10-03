using System;
using System.Collections.Generic;
using UnityEngine;


namespace FluidDemo
{
    public class ParticleVisualization : MonoBehaviour
    {
        public GameObject liquidParticlePrefab;
        public GameObject greenLiquidParticlePrefab;
        public GameObject redLiquidParticlePrefab;
        public GameObject gasParticlePrefab;
        public GameObject solidParticlePrefab;

        private Dictionary<int, GameObject> _particles;
        private ParticleSystem _particleSystem; 

        private void Awake()
        {
            _particles = new Dictionary<int, GameObject>();
            _particleSystem = GetComponent<ParticleSystem>();
        }

        public void AddParticle(int id, FluidId fluidId, Vector2 position)
        {
            if (_particles.ContainsKey(id))
            {
                Debug.LogWarning("Particle with id " + id + " already exists in the visualization.");
                return;
            }

            var particle = Instantiate(PrefabFor(fluidId), parent: transform, worldPositionStays: false);
            particle.name = "Particle " + id.ToString();
            _particles.Add(id, particle);

            particle.transform.position = new Vector3(position.x, position.y, 0f);

/*
            var emitParams = new ParticleSystem.EmitParams
            {
                position = new Vector3(position.x, position.y, 0f),
            };

            _particleSystem.Emit(emitParams, 1);
*/
            
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


/*
            Particle[] particles = particleEmitter.particles;

            // Do changes
            for (int i = 0; i < particles.Length; i++)
            {
                particles<em>.position = Vector3.MoveTowards(particleEmitter.particles_.position,
                    spot.transform.position, Time.deltaTime * speed);
                _ </em >
            }
            
            */
        }

        public void ColorParticle(int id, Color color)
//            => _particles[id].GetComponentInChildren<SpriteRenderer>().color = color;
        {}      
        
        public Color RandomColor => new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }
}