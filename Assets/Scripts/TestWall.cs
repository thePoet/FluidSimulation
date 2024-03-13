using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
   public class TestWall : MonoBehaviour
    {
       
        
        void Start()
        {
            var simulation = FindObjectOfType<Simulation>();
            simulation.SpawnParticle(transform.position, Vector2.zero, ParticleType.Solid);
        }
    }
}