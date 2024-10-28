using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{
    public class ParticleCollection : SpannableDictionary<ParticleId, Particle>
    {
        public ParticleCollection(int maxNumEntries) : base(maxNumEntries) { }
        
        public void Add(Particle particle) =>  base.Add(particle.Id, particle);
        public void Update(Particle particle) => base.Update(particle.Id, particle);
    }
}