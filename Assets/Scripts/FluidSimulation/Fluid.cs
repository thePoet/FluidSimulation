namespace FluidSimulation
{
    // TODO: Muuta tyypeiksi jotka enforcaa jai informoi sallitun rangen 
    
    public abstract record Fluid(float Density);

    public record Liquid(float Density, float Viscosity) : Fluid(Density);
    public record Gas(float Density, float Viscosity)    : Fluid(Density);
    public record Solid(float Density)                   : Fluid(Density);  // Non-fluid fluid :)
}
