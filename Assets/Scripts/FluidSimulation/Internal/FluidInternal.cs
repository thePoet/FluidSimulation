namespace FluidSimulation.Internal
{
    public struct FluidInternal
    {
        public const int Stride = sizeof(int) + 8 * sizeof(float);

        public int State;  // 0=Liquid, 1=Gas, 2=Solid
        public float Stiffness;
        public float NearStiffness;
        public float RestDensity;
        public float ViscositySigma;
        public float ViscosityBeta;
        public float GravityScale;
        public float Mass;
        public float DensityPullFactor;
    }
}
