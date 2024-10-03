namespace FluidSimulation
{
    public struct ProximityAlertSubscription
    {
        public int IndexFluidA;
        public int IndexFluidB;
        public float Range;
        
        public const int Stride = 2 * sizeof(int) + sizeof(float); 
    }
}