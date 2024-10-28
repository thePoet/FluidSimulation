using System;
using UnityEngine;
using FluidSimulation;
using RikusGameDevToolbox.GeneralUse;
using TMPro;


namespace FluidDemo
{
    public class Simulation : MonoBehaviour
    {
        public TextMeshPro text;
        
        private FluidDynamics _fluidDynamics;
        private ParticleCollection _particles;
        private SpatialPartitioningGrid<ParticleId> _partitioningGrid;
        private ParticleVisuals _visuals;

        private FluidSimParticle[] _fspBuffer; 

        private MovingAverage _avgUpdateTime;
        
        private SimulationSettings Settings => new()
        {
            Scale = 6f,
            Gravity = 1200f,
            MaxNumParticles = 30000,
            IsViscosityEnabled = true,
            AreaBounds = new Rect(Vector2.zero, new Vector2(1200f, 600f)),
            SolidRadius = 15f
        };
        
        private const int MaxNumParticlesInPartitioningSquare = 40;
        
        public ParticleId[] ParticlesInsideRectangle(Rect rect) => _partitioningGrid.RectangleContents(rect);
        public ParticleId[] ParticlesInsideCircle(Vector2 position, float radius) => _partitioningGrid.CircleContents(position, radius);
        public Vector2[] ParticleDebugData() => _fluidDynamics.DebugData();
        public void SelectDebugParticle(ParticleId id) => _fluidDynamics.SubscribeDebugData(_particles.SpanIndexOf(id));
     
        #region ------------------------------------------ UNITY METHODS ----------------------------------------------
       
        private void Awake()
        {
            _visuals = FindObjectOfType<ParticleVisuals>();
            if (_visuals == null) Debug.LogError("No visualization found in the scene.");
            var alerts = CreateProximityAlertSubscriptions();
            _fluidDynamics = new FluidDynamics(Settings, Substances.List, alerts, 500);
            _fspBuffer = CreateFluidSimParticleBuffer(Settings.MaxNumParticles);
            _partitioningGrid = CreateSpatialPartitioningGrid();
            _particles = new ParticleCollection(Settings.MaxNumParticles);
            _avgUpdateTime = new MovingAverage(300);
        }


        void Update()
        {
            float t = Time.realtimeSinceStartup;
           
            SimulateFluids(0.015f, _fluidDynamics, _particles);
            DoSpatialPartitioning(_partitioningGrid, _particles);
            DoChemicalReactions();
            MoveParticleVisuals();
            
            t = Time.realtimeSinceStartup - t;
            _avgUpdateTime.Add(t);
            float avgUpdate = _avgUpdateTime.Average()*1000f;

            text.text = "Particles: " + _particles.Count + " - " + avgUpdate.ToString("0.0") +
                        " ms.";
        }

        private void OnDisable()
        {
            _fluidDynamics.Dispose();
        }

        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        

        public Particle SpawnParticle(Vector2 position, Vector2 velocity, SubstanceId substanceId)
        {
            var particle = new Particle();
            particle.Id = ParticleId.CreateUnique();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.SubstanceId = substanceId;
            particle.Visuals = _visuals.Create(particle);
            _particles.Add(particle);
            
            return particle;
        }
        
        public Particle GetParticle(ParticleId id)
        {
            return _particles.Get(id);
        }

        public void UpdateParticle(Particle particle)
        {
            if (IsFluidChanged())
            {
                _visuals.DestroyVisuals(particle);
                particle.Visuals = _visuals.Create(particle);
            }

            _particles.Update(particle);
            
            bool IsFluidChanged() =>_particles.Get(particle.Id).SubstanceId != particle.SubstanceId;
        }
        
        public void DestroyParticle(ParticleId id)
        {
            var p = _particles.Get(id);
            _visuals.DestroyVisuals(p);
            _particles.Remove(id);
        }

        public void Clear()
        {
            DestroyAllVisuals();
            _particles.Clear();
        }


        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void SimulateFluids(float deltaTime, FluidDynamics fluidDynamics, ParticleCollection particles)
        {
            var span = particles.AsSpan();
            CopySpanToBuffer(span);
            fluidDynamics.Step(deltaTime, _fspBuffer);
            CopeBufferToSpan(span);


            void CopySpanToBuffer(Span<Particle> span)
            {
                for (int i=0; i<Settings.MaxNumParticles; i++)
                {
                    if (i < span.Length)
                    {
                        _fspBuffer[i].Active = true;
                        _fspBuffer[i].Position = span[i].Position;
                        _fspBuffer[i].Velocity = span[i].Velocity;
                        _fspBuffer[i].SetFluid(span[i].SubstanceId);
                    }
                    else
                    {
                        _fspBuffer[i].Active = false;
                    }
                }
            }

            void CopeBufferToSpan(Span<Particle> span)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Position = _fspBuffer[i].Position;
                    span[i].Velocity = _fspBuffer[i].Velocity;
                }
            }
        }

        private FluidSimParticle[] CreateFluidSimParticleBuffer(int size)
        {
            var buffer = new FluidSimParticle[Settings.MaxNumParticles];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = new FluidSimParticle
                {
                    Position = Vector2.zero,
                    Velocity = Vector2.zero,
                    SubstanceIndex = -1,
                    Active = false
                };
            }

            return buffer;
        }
        

        private ProximityAlertRequest[] CreateProximityAlertSubscriptions()
        {
            var pas = new ProximityAlertRequest
            {
                IndexFluidA = Substances.IndexOf(SubstanceId.GreenLiquid),
                IndexFluidB = Substances.IndexOf(SubstanceId.RedLiquid),
                Range = 10f
            };

            return new[]{pas};
        }
        
        private (ParticleId, ParticleId)[] GetProximityAlerts()
        {
            int numAlerts = _fluidDynamics.ProximityAlerts.Length;
            var result = new (ParticleId, ParticleId)[numAlerts];
            var span = _particles.AsSpan();
            
            for (int i=0; i<numAlerts; i++)
            {
                var alert = _fluidDynamics.ProximityAlerts[i];
                result[i] = (span[alert.IndexParticleA].Id, span[alert.IndexParticleB].Id);
            }
        
            return result;
        }

        private void DoChemicalReactions()
        {
            foreach (var (pId1, pId2) in GetProximityAlerts())
            {
                var p1 = GetParticle(pId1);
                var p2 = GetParticle(pId2);
                
                if (p1.SubstanceId == SubstanceId.GreenLiquid && p2.SubstanceId == SubstanceId.RedLiquid)
                {
                   p1.SubstanceId = SubstanceId.Smoke;
                   p2.SubstanceId = SubstanceId.Water;
                   UpdateParticle(p1);
                   UpdateParticle(p2);
                }
            }
        }
        

        private void MoveParticleVisuals()
        {
            foreach (var particle in _particles.AsSpan())
            {
                if (particle.Visuals is null) continue;
                particle.Visuals.transform.position = particle.Position;
            }
        }

        private void DestroyAllVisuals()
        {
            foreach (var particle in _particles.AsSpan())
            {
                _visuals.DestroyVisuals(particle);
            }
        }
        
        private SpatialPartitioningGrid<ParticleId> CreateSpatialPartitioningGrid()
        {
            float squareSize = 3.5f * Settings.Scale;
            return new SpatialPartitioningGrid<ParticleId>(Settings.AreaBounds, squareSize, MaxNumParticlesInPartitioningSquare);
        }

        private void DoSpatialPartitioning(SpatialPartitioningGrid<ParticleId> grid, ParticleCollection particles)
        {
            grid.Clear();
            foreach (var particle in particles.AsSpan())
            {
                grid.Add(particle.Id, particle.Position);
            }
        }
        
        #endregion


    }
}


