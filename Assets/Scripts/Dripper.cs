using System.Collections;
using System.Collections.Generic;
using FluidSimulation;
using UnityEngine;

public class Dripper : MonoBehaviour
{
    private TestFluidDynamics _testFluidDynamics;
    // Start is called before the first frame update
    void Start()
    {
        _testFluidDynamics = FindObjectOfType<TestFluidDynamics>();
        InvokeRepeating("SpawnParticle", 0.1f,1f);
    }

    // Update is called once per frame
    void SpawnParticle()
    {
        _testFluidDynamics.SpawnParticle(transform.position, Vector2.zero, FluidSubstance.SomeLiquid);
    }
}
