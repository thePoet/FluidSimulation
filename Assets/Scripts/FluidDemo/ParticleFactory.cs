using UnityEngine;

namespace FluidDemo
{
    /*
    public static class ParticleFactory
    {
        private static WorldParticles _particles;
        private static ParticleVisuals _visuals;
        
        public static void Initialize(WorldParticles particles, ParticleVisuals visuals)
        {
            _particles = particles;
            _visuals = visuals;
        }

        public static Particle CreateParticle(FluidId fluidId, Vector2 position)
        {
            var particle = new Particle(position, fluidId, ParticleId.New(_particles));
            _particles.Add(particle);
            return particle;
        }
        
        public static void DeleteParticle(Particle particle)
        {
            _particles.Remove(particle);
        }
        
        public static void DeleteParticle(ParticleId particleId)
        {
            var p = _particles.Get(particleId);
            _particles.Remove(p);
        }

    }*/
}