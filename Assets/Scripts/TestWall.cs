using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
   public class TestWall : MonoBehaviour
   {
        private int particleId;
        private Simulation simulation;
        
        void Start()
        {
            simulation = FindObjectOfType<Simulation>();
            particleId = simulation.SpawnParticle(transform.position, Vector2.zero, ParticleType.Solid);
        }

        private void Update()
        {
            simulation.MoveParticle(particleId, transform.position);
        }
   }
}