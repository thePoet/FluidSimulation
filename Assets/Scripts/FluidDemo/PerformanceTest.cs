namespace FluidDemo
{
    public class PerformanceTest
    {
        /*
        private void RunPerformanceTest()
        {
            Clear();

            Random.InitState(123);

            for (int i = 0; i < 4000; i++)
            {
                SpawnParticle(RandomPosition(), Vector2.zero, FluidSubstance.SomeLiquid);
            }

            Timer timer = new Timer();
            for (int i = 0; i < 60; i++) _fluidDynamics.Step(_particleData, 0.015f);
            Debug.Log("Performance test took " + timer.Time * 1000f + " ms.");

            Vector2 RandomPosition()
            {
                return new Vector2
                (
                    x: Random.Range(SimulationSettings.AreaBounds.xMin, SimulationSettings.AreaBounds.xMax),
                    y: Random.Range(SimulationSettings.AreaBounds.yMin, SimulationSettings.AreaBounds.yMax)
                );
            }
        }
*/
    }
}