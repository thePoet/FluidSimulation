namespace FluidSimulation
{
    //
    /// <summary>
    /// Requests an alert when particles givens substances come close to each other
    /// </summary>
    public struct ProximityAlertRequest
    {
        public int IndexFluidA;
        public int IndexFluidB;
        public float Range;
    }
}