using System;
using RikusGameDevToolbox.GeneralUse;

namespace FluidSimulation
{
    public abstract record Fluid(float Density);

    public record Liquid(float Density, float Viscosity) : Fluid(Density);
    public record Gas(float Density, float Viscosity)    : Fluid(Density);
    public record Solid(float Density)                   : Fluid(Density);  // Solid is a non-fluid fluid :)

}
