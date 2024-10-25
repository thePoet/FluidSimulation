using UnityEngine;
using FluidSimulation;

namespace FluidDemo
{
    public struct Particle
    {
        private static ParticleCollection _particleCollection;
        private static FluidSimParticle[] _fsParticles;
        private static ParticleVisuals _particleVisuals; // TODO: pois?


        
        public ParticleId Id { get; init; }
        public int FluidSimParticleIdx;
        public GameObject Visualization;


        public Vector2 Position
        {
            get => _fsParticles[FluidSimParticleIdx].Position;
            set => SetPosition(value);
        }

        public Vector2 Velocity
        {
            get => _fsParticles[FluidSimParticleIdx].Velocity;
            set => SetVelocity(value);
        }
        public FluidId FluidId
        {
            get => _fsParticles[FluidSimParticleIdx].GetFluid();
            set => SetFluid(value);
        }

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public static void Initialize(ParticleCollection particleCollection,  ParticleVisuals particleVisuals)
        {
            _particleCollection = particleCollection;
            _fsParticles = CreateFluidSimParticleArray(particleCollection.MaxNumParticles);
            _particleVisuals = particleVisuals;
        }
 
        
        public Particle(FluidId fluidId, Vector2 position)
        {
            var fsp = new FluidSimParticle
            {
                Position = position,
                Velocity = Vector2.zero,
                SubstanceIndex = Fluids.IndexOf(fluidId),
                Id = -1,
                Active = true
            };

            FluidSimParticleIdx = _fsParticles.Add(fsp);
            
            Visualization = _particleVisuals.Create(fluidId, position);

            Id = ParticleId.New(_particleCollection);
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
            var fsp = _fsParticles[FluidSimParticleIdx];
            fsp.Position = position;
            _fsParticles[FluidSimParticleIdx] = fsp;
            UpdateVisualPosition();
        }
        
        private void SetVelocity(Vector2 velocity)
        {
            var fsp = _fsParticles[FluidSimParticleIdx];
            fsp.Velocity = velocity;
            _fsParticles[FluidSimParticleIdx] = fsp;
        }
        
        private void SetFluid(FluidId fluidId)
        {
            var fsp = _fsParticles[FluidSimParticleIdx];
            fsp.SubstanceIndex = Fluids.IndexOf(fluidId);
            _fsParticles[FluidSimParticleIdx] = fsp;
                
            ChangeSubstanceInVisualization(fluidId);
        }
        
        private void ChangeSubstanceInVisualization(FluidId newFluidId)
        {
            if (Visualization != null) Object.Destroy(Visualization);
            Visualization = _particleVisuals.Create(newFluidId, Position);
        }
        /*
        private static FluidSimParticle[] CreateFluidSimParticleArray(int maxNumParticles)
        {
            var fsParticles = new FluidSimParticle[maxNumParticles];
            for (int i = 0; i < fsParticles.Length; i++)
            {
                fsParticles[i] = new FluidSimParticle
                {
                    Position = Vector2.zero,
                    Velocity = Vector2.zero,
                    SubstanceIndex = -1,
                    Id = -1,
                    Active = false
                };
            }

            return fsParticles;
        }
      */
            
            
        #endregion


    }
}
