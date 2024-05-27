using System.Collections;
using System.Collections.Generic;
using FluidSimulation;
using UnityEngine;

public interface IParticleDynamics 
{
    void Step(IParticleData particleData, float deltaTime);
}
