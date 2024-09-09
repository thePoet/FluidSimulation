using System;
using UnityEngine;



namespace FluidSimulation
{
    
    public class FluidDynamics
    {
        public ComputeShader ComputeShader;
        public int NumParticles { get; private set; }

        public FluidsComputeShader _computeShader;
        private readonly FluidParticle[] _particles;
        private int _nextId = 1;
        private SpatialPartitioningGrid<int> _partitioningGrid;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public FluidDynamics(SimulationSettings settings, Fluid[] fluids)
        {
            _computeShader = new FluidsComputeShader("FluidDynamicsComputeShader", settings, fluids);
            _particles = new FluidParticle[settings.MaxNumParticles];


            _partitioningGrid = new SpatialPartitioningGrid<int>(
                settings.PartitioningGrid,
                settings.MaxNumParticlesInPartitioningCell,
                i => _particles[i].Position);
        }
        
        
 
        public void EndSimulation()
        {
            _computeShader.Dispose();
        }
        public void Step(float timeStep)
        {
            _computeShader.Step(timeStep, _particles, NumParticles);
            UpdateSpatialPartitioningGrid();
        }
        public void SelectParticle(int particleId)
        {
            _computeShader.SelectedParticle = particleId;
        }

        public Span<FluidParticle> Particles => _particles.AsSpan().Slice(0, NumParticles);
        
        public int AddParticle(FluidParticle particle)
        {
            particle.Id = _nextId;
            _nextId++;
            NumParticles++;
            int index = NumParticles - 1;
            _particles[index] = particle;
            return particle.Id;
        }
        
        public void RemoveParticle(int particleIndex)
        {
            throw new NotImplementedException();
        }
        
        public int[] ParticlesInsideCircle(Vector2 position, float radius) => _partitioningGrid.CircleContents(position, radius);

        public void Clear()
        {
            NumParticles = 0;
        }    

        #endregion

        // TODO: Read from compute buffer instead.
        private void UpdateSpatialPartitioningGrid()
        {
            _partitioningGrid.Clear();
      
            for (int i = 0; i < NumParticles; i++)
            {
                _partitioningGrid.Add(i);
            }
        }


     
    }
}
