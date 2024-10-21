using FluidSimulation;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{
    public class Demo : MonoBehaviour
    {
        NewParticles _particles;
        
        private SimulationSettings Settings => new()
        {
            Scale = 6f,
            Gravity = 1200f,
            MaxNumParticles = 30000,
            IsViscosityEnabled = true,
            AreaBounds = new Rect(Vector2.zero, new Vector2(1200f, 600f)),
            SolidRadius = 15f
        };
        
        void Awake()
        {
         
            _particles = new NewParticles(30000, new Grid2D(Settings.AreaBounds, squareSize: 3.5f * Settings.Scale));
        }


        void Update()
        {
            MoveSolids();
        }

        private void MoveSolids()
        {
        
            //_particles.Get(particleId).Move();
        }
    }
}