namespace FluidSimulation
{
    public struct FluidInternal
    {
        public int StateIndex;
        public float Stiffness;
        public float NearStiffness;
        public float RestDensity;
        public float ViscositySigma;
        public float ViscosityBeta;
        public float GravityScale;
        public float Mass;
        public float DensityPullFactor;

        public static int Stride => sizeof(int) + 8 * sizeof(float);


        public State State
        {
            get => (State)StateIndex;
            set => StateIndex = (int)value;
        }
        
        
    }

    public enum State
    {
        Liquid,
        Gas,
        Solid,
    }
}