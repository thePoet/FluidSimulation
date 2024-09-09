using FluidSimulation;
using UnityEngine;

public class Dripper : MonoBehaviour
{
    public bool onlyOne = false;
    
    private TestFluidDynamics _testFluidDynamics;
    // Start is called before the first frame update
    void Start()
    {
        _testFluidDynamics = FindObjectOfType<TestFluidDynamics>();
        if (onlyOne)
            SpawnParticle();
        else
            InvokeRepeating("SpawnParticle", 0.1f,1f);
    }

    // Update is called once per frame
    void SpawnParticle()
    {
        _testFluidDynamics.SpawnParticle(transform.position, Vector2.zero, FluidSubstance.SomeLiquid);
    }
}
