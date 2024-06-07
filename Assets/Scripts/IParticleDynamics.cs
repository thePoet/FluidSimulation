using FluidSimulation;

public interface IParticleDynamics 
{
    void Step(IParticleData particleData, float deltaTime);
}
