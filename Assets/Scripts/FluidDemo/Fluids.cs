using FluidSimulation;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{    
    public enum FluidId
    {
        Water,
        Smoke,
        Rock,
        GreenLiquid,
        RedLiquid
    }

    public static class Fluids
    {
        public static readonly Fluid[] List;
        
        static Fluids()
        {
            int numLiquids = Generic.NumEnumerators<FluidId>();
            List = new Fluid[numLiquids];
            
            List[(int)FluidId.Water] = new Liquid(Density: 1f, Viscosity: 0.3f);
            List[(int)FluidId.Smoke] = new Gas(Density: 0.01f,  Viscosity: 0.1f);
            List[(int)FluidId.Rock]  = new Solid(Density: 2f);
            List[(int)FluidId.GreenLiquid] = new Liquid(Density: 1f, Viscosity: 0.3f);
            List[(int)FluidId.RedLiquid] = new Liquid(Density: 1f, Viscosity: 0.3f);
        }
      
        public static int IndexOf(FluidId fluidId) => (int)fluidId;

        // Extension methods for FluidParticle so we can get and set it's fluid with FluidId
        public static FluidId GetFluid(this FluidParticle particle) => (FluidId)particle.FluidIndex;
        public static void SetFluid(this ref FluidParticle particle, FluidId id) => particle.FluidIndex = IndexOf(id);

    }
}