using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public class Particle
    {
        public static Particles Particles;
        public static ParticleVisualization ParticleVisualization; // TODO: pois

        public int FluidSimParticleIdx;
        public GameObject Visualization;


        public Vector2 Position
        {
            get => Particles[FluidSimParticleIdx].Position;
            set => SetPosition(value);
        }

        public Vector2 Velocity
        {
            get => Particles[FluidSimParticleIdx].Velocity;
            set => SetVelocity(value);
        }
        public FluidId FluidId
        {
            get => Particles[FluidSimParticleIdx].GetFluid();
            set => SetFluid(value);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

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

            FluidSimParticleIdx = Particles.Add(fsp);
            
            Visualization = ParticleVisualization.Create(fluidId, position);
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
            var fsp = Particles[FluidSimParticleIdx];
            fsp.Position = position;
            Particles[FluidSimParticleIdx] = fsp;
            UpdateVisualPosition();
        }
        
        private void SetVelocity(Vector2 velocity)
        {
            var fsp = Particles[FluidSimParticleIdx];
            fsp.Velocity = velocity;
            Particles[FluidSimParticleIdx] = fsp;
        }
        
        private void SetFluid(FluidId fluidId)
        {
            var fsp = Particles[FluidSimParticleIdx];
            fsp.SubstanceIndex = Fluids.IndexOf(fluidId);
            Particles[FluidSimParticleIdx] = fsp;
                
            ChangeSubstanceInVisualization(fluidId);
        }
        
        private void ChangeSubstanceInVisualization(FluidId newFluidId)
        {
            if (Visualization != null) Object.Destroy(Visualization);
            Visualization = ParticleVisualization.Create(newFluidId, Position);
        }
        
        #endregion


    }
}
