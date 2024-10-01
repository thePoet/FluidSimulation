using FluidSimulation;

namespace FluidDemo
{    
    public enum FluidId
    {
        Water,
        Smoke,
        Rock
    }
    
    public static class Fluids
    {
        public static readonly Fluid[] List; 

        static Fluids()
        {
            List = new Fluid[3];
            List[(int)FluidId.Water] = new Liquid(Density: 1f, Viscosity: 0.3f);
            List[(int)FluidId.Smoke] = new Gas(Density: 0.1f,  Viscosity: 0.1f);
            List[(int)FluidId.Rock]  = new Solid(Density: 2f);
        }
      
        
        public static int IndexOf(FluidId fluidId) => (int)fluidId;

        public static FluidId IdFluid(this FluidParticle particle) => (FluidId)particle.FluidIndex;
        
        
    }
}