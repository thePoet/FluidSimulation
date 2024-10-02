namespace FluidSimulation.Internal
{
    public struct FluidInternal
    {
        public int State;  // 0=Liquid, 1=Gas, 2=Solid
        public float Stiffness;
        public float NearStiffness;
        public float RestDensity;
        public float ViscositySigma;
        public float ViscosityBeta;
        public float GravityScale;
        public float Mass;
        public float DensityPullFactor;

        public static int Stride => sizeof(int) + 8 * sizeof(float);

        public static FluidInternal From(Fluid fluid)
        {
            var f = new FluidInternal();
            if (fluid is Liquid)
            {
                f.State = 0;
                f.Stiffness = 2000f;
                f.NearStiffness = 4000f;
                f.RestDensity = 5f;
                f.DensityPullFactor = 0.5f;

                f.ViscositySigma = 0.2f * (fluid as Liquid).Viscosity;
                f.ViscosityBeta = 0.2f * (fluid as Liquid).Viscosity;

                f.GravityScale = 1f;
            }

            if (fluid is Gas)
            {
                f.State = 1;
                f.Stiffness = 200f;
                f.NearStiffness = 400f;
                f.RestDensity = 5f;
                f.DensityPullFactor = 1f;
                
                f.ViscositySigma = 0.2f * (fluid as Gas).Viscosity;
                f.ViscosityBeta = 0.2f * (fluid as Gas).Viscosity;

                f.GravityScale = -0.05f;
            }

            if (fluid is Solid)
            {
                f.State = 2;
                f.Stiffness = 1f;
                f.NearStiffness = 1f;
                f.RestDensity = 1f;
                f.DensityPullFactor = 0f;

                f.GravityScale = 0f;
            }
        
            f.Mass = fluid.Density;
                
            return f;
        }
    }
}
