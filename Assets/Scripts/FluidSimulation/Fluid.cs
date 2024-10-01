namespace FluidSimulation
{
    // TODO: Muuta tyypeiksi jotka enforcaa jai informoi sallitun rangen 
    
    public abstract record Fluid(string Name, float Density);

    public record Liquid(string Name, float Density, float Viscosity) : Fluid(Name, Density);
    public record Gas(string Name, float Density, float Viscosity)    : Fluid(Name, Density);
    public record Solid(string Name, float Density)                   : Fluid(Name, Density);  // Non-fluid fluid :)
}
