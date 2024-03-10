using System;

namespace FluidSimulation
{
    public class ParticleData : IParticleData
    {
        private readonly int _maxNumParticles;
        private readonly int _maxNumNeighbours;
        private int _numParticles = 0;
        
        private readonly float _neighbourRadius;
        private readonly FluidParticle[] _particles;
        private readonly int[][] _neighbours;
        private readonly int[] _neighbourCount;
        private readonly (int, int)[] _particlePairs;
        private int _nextId = 0;
        private readonly SpatialPartitioning _partitioning;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        public ParticleData(int maxNumParticles, int maxNumNeighbours, float neighbourRadius)
        {
            _maxNumParticles = maxNumParticles;
            _maxNumNeighbours = maxNumNeighbours;
            _neighbourRadius = neighbourRadius;

            
            _particles = new FluidParticle[maxNumParticles];
            
            _neighbours = new int[maxNumParticles][];
            _neighbourCount = new int[maxNumParticles];
            for (int i = 0; i < maxNumParticles; i++)
            {
                _neighbours[i] = new int[maxNumNeighbours];
                _neighbourCount[i] = 0;
            }
       
            int maxNumParticlePairs = (int)(maxNumParticles * maxNumNeighbours * 0.5f) + 1;
            _particlePairs = new (int, int)[maxNumParticlePairs];
            
            
            _partitioning = new SpatialPartitioning(neighbourRadius, maxNumNeighbours);
        }

        

        // Returns id number of the added particle
        public int Add(FluidParticle particle)
        {
            particle.PreviousPosition = particle.Position;
            
            particle.Id = _nextId;
            _nextId++;
            
            _numParticles++;
            int index = _numParticles - 1;
            
            _particles[index] = particle;
            _neighbourCount[index] = 0;
            
            _partitioning.AddEntity(index, particle.Position);
            return particle.Id;
        }

        public void Remove(int particleIndex)
        {
            throw new NotImplementedException();
        }

        public Span<FluidParticle> All()
        {
            var span = (Span<FluidParticle>)_particles;
            return span.Slice(0, _numParticles);
        }

        public Span<int> NeighbourIndices(int particleIndex)
        {
            var span =  (Span<int>)_neighbours[particleIndex];
            return span.Slice(0, _neighbourCount[particleIndex]);
        }

        public Span<(int, int)> NeighbourParticlePairs()
        {
            int p = 0;
            
            Span<(int, int)> pairs = _particlePairs;
            
            for (int indexA = 0; indexA < _numParticles; indexA++)
            {
                for (int i = 0; i < _neighbourCount[indexA]; i++)
                {
                    int indexB = _neighbours[indexA][i];
                    if (_particles[indexA].Id > _particles[indexB].Id)
                    {
                        pairs[p] = (indexA, indexB);
                        p++;
                    }
                }
            }

            return pairs.Slice(0, p);
        }
       
        public void UpdateNeighbours()
        {
            for (int i = 0; i < _numParticles; i++)
                _partitioning.UpdateEntity(i, _particles[i].Position);
            
            for (int i = 0; i < _numParticles; i++)
            {
                int numNeighbours = _partitioning.WriteEntiesInNeighourhoodTo(_neighbours[i], _particles[i].Position);
                _neighbourCount[i] = numNeighbours;
            }
            
        }
       
        public void Clear()
        {
            _numParticles = 0;
            _partitioning.Clear();
        }
        
        
        public int NumberOfParticles => _numParticles;
        
        
        #endregion

  

        
    }
}
