using System;

namespace FluidSimulation
{
    public abstract record Substance(Density Density);

    public abstract record Fluid(Density Density, Viscosity Viscosity) : Substance(Density);
    public record Liquid(Density Density, Viscosity Viscosity) : Fluid(Density, Viscosity);
    public record Gas(Density Density, Viscosity Viscosity)    : Fluid(Density, Viscosity);
    public record Solid(Density Density)                       : Substance(Density);  

    /// <summary>
    /// Density of a substance. Must be between 0f and 10f.
    /// </summary>
    public record Density 
    {
        public float Value { get; }

        public Density(float value)
        {
            if (value is < 0f or > 10f) throw new ArgumentException("Density value must be between 0f and 10f");
            Value = value;
        }

        public static implicit operator float(Density d) => d.Value;
        public static implicit operator Density(float value) => new(value);
    }
    
    /// <summary>
    /// Viscosity of gas or solid. Must be between 0f and 1f.
    /// </summary>
    public record Viscosity
    {
        public float Value { get; }

        public Viscosity(float value)
        {
            if (value is < 0f or > 1f) throw new ArgumentException("Viscosity value must be between 0f and 1f");
            Value = value;
        }

        public static implicit operator float(Viscosity v) => v.Value;
        public static implicit operator Viscosity(float value) => new(value);
    }



}

