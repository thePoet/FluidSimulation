using FluidSimulation;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{    
    
    public static class Substances
    {
        public static readonly Substance[] List;
        
        static Substances()
        {
            int numLiquids = Generic.NumEnumerators<SubstanceId>();
            List = new Substance[numLiquids];
            
            List[(int)SubstanceId.Water] = new Liquid(Density: 1f, Viscosity: 0.3f);
            List[(int)SubstanceId.Smoke] = new Gas(Density: 0.05f,  Viscosity: 0.1f);
            List[(int)SubstanceId.Rock]  = new Solid(Density: 2f);
            List[(int)SubstanceId.GreenLiquid] = new Liquid(Density: 1f, Viscosity: 0.3f);
            List[(int)SubstanceId.RedLiquid] = new Liquid(Density: 1f, Viscosity: 0.3f);
        }
      
        public static int IndexOf(SubstanceId substanceId) => (int)substanceId;

        // Extension methods for FluidParticle so we can get and set it's fluid with FluidId
        public static SubstanceId GetFluid(this FluidSimParticle particle) => (SubstanceId)particle.SubstanceIndex;
        public static void SetFluid(this ref FluidSimParticle particle, SubstanceId id) => particle.SubstanceIndex = IndexOf(id);
    

    }
}