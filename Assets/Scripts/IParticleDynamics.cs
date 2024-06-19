using FluidSimulation;

public interface IParticleDynamics 
{
    void Step(ParticleData particleData, float deltaTime);
}
