using System.Collections.Generic;
using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public class Particle
    {
        public static Particles FsParticles;
        public static ParticleVisuals ParticleVisuals; // TODO: pois


        public static List<Particle> particles;
        
        public int FluidSimParticleIdx;
        public GameObject Visualization;


        public Vector2 Position
        {
            get => FsParticles[FluidSimParticleIdx].Position;
            set => SetPosition(value);
        }

        public Vector2 Velocity
        {
            get => FsParticles[FluidSimParticleIdx].Velocity;
            set => SetVelocity(value);
        }
        public FluidId FluidId
        {
            get => FsParticles[FluidSimParticleIdx].GetFluid();
            set => SetFluid(value);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        static Particle()
        {
            particles = new List<Particle>();
        }
        
        public Particle(Vector2 position, FluidId fluidId)
        {
            var fsp = new FluidSimParticle
            {
                Position = position,
                Velocity = Vector2.zero,
                SubstanceIndex = Fluids.IndexOf(fluidId),
                Id = -1,
                Active = true
            };

            FluidSimParticleIdx = FsParticles.Add(fsp);
            
            Visualization = ParticleVisuals.Create(fluidId, position);
            particles.Add(this);
        }

        public void UpdateVisualPosition()
        {
            if (Visualization == null) return;
            Visualization.transform.position = new Vector3(Position.x, Position.y, 0f);
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void SetPosition(Vector2 position)
        {
            var fsp = FsParticles[FluidSimParticleIdx];
            fsp.Position = position;
            FsParticles[FluidSimParticleIdx] = fsp;
            UpdateVisualPosition();
        }
        
        private void SetVelocity(Vector2 velocity)
        {
            var fsp = FsParticles[FluidSimParticleIdx];
            fsp.Velocity = velocity;
            FsParticles[FluidSimParticleIdx] = fsp;
        }
        
        private void SetFluid(FluidId fluidId)
        {
            var fsp = FsParticles[FluidSimParticleIdx];
            fsp.SubstanceIndex = Fluids.IndexOf(fluidId);
            FsParticles[FluidSimParticleIdx] = fsp;
                
            ChangeSubstanceInVisualization(fluidId);
        }
        
        private void ChangeSubstanceInVisualization(FluidId newFluidId)
        {
            if (Visualization != null) Object.Destroy(Visualization);
            Visualization = ParticleVisuals.Create(newFluidId, Position);
        }
        
        #endregion


    }
}
