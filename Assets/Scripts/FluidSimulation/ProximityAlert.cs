namespace FluidSimulation
{
    public struct ProximityAlert
    {
        public int IndexParticleA;
        public int IndexParticleB;
        public int RequestIndex;
        
        public static int Stride => sizeof(int) * 3;
    }
}