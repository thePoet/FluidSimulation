


namespace FluidSimulation
{
    //
    /// <summary>
    /// Requests an alert when particles givens substances come close to each other
    /// </summary>
    public struct ProximityAlertRequest
    {
        public int IndexSubstanceA;
        public int IndexSubstanceB;
        public float Range;
    }
}