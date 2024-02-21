using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;

// Based on paper by Miles Macklin and Matthias Müller
// https://mmacklin.com/pbf_sig_preprint.pdf

// useful reference: https://github.com/yuki-koyama/position-based-fluids/blob/main/src/main.cpp
namespace Elemental
{
    
    
    public class LiquidSimulation : MonoBehaviour
    {
 
        private static List<LiquidParticle> _particles;
  
        [FormerlySerializedAs("myTexture")] public Texture2D densityTexture;
        public Rect bounds;
        public int numSolverIterations;
        public float interactionDistance;
        public float restDensity;
        public float particleMass;
        public float relaxationParameter;
        public float gravity;
        
        
        public float timeAll;
        public float timeNeighbours;
        
        private SmoothingKernel2D _smoothing;
        private MovingAverage timeAvgCalc;
        private MovingAverage timeAvgNeighbours;
        private MovingAverage timeAvgAll;
        private Timer timer;
        private Timer timerAll;
        
        static LiquidSimulation()
        {
            _particles = new List<LiquidParticle>();
        }
        
        public static void AddParticle(LiquidParticle particle)
        {
            _particles.Add(particle);
        }
        
        public static List<LiquidParticle> GetLiquidParticlesInCircle(Vector2 center, float radius)
        {
            var colliders = Physics2D.OverlapCircleAll(center, radius);
            return colliders.Where(c => c.GetComponent<LiquidParticle>() != null)
                .Select(c => c.GetComponent<LiquidParticle>()).ToList();
        }

        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        private void Awake()
        {
            _smoothing = new SmoothingKernel2D(interactionDistance);
            _smoothing.Test();
            timeAvgNeighbours = new MovingAverage(50);
            timeAvgAll = new MovingAverage(50);
            timerAll = new Timer();
            timer = new Timer();

            densityTexture = new Texture2D(100, 100);
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    densityTexture.SetPixel(i, j, Color.red);
                }
            }
            densityTexture.Apply();
        }

       
        
        private void Update()
        {
            Simulate(0.015f);
            
            
            if (Input.GetKeyDown(KeyCode.N)) Debug.Log("Number of particles: " + _particles.Count);
            if (Input.GetKeyDown(KeyCode.D))
            {
                // get mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Debug.Log("Density: " + DensityAt(mousePosition));
            }
        }
        
      

        void OnDrawGizmos()
        {
            // Draw a texture rectangle on the XY plane of the scene
            Gizmos.DrawGUITexture(bounds, densityTexture);
        }
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        void Simulate(float timeStep)
        {
            
            timerAll.Reset();
            
            _particles.ForEach(p => p.startPosition = p.Position);

            _particles.ForEach(p => ApplyExternalForcesTo(p, timeStep));
            _particles.ForEach(p => p.Position += p.velocity * timeStep);

            _particles.ForEach(p => p.neighbours = NeighboursOf(p));
           
            for (int i = 0; i < numSolverIterations; i++)
            {
                _particles.ForEach(p => p.ScalingFactor = ScalingFactor(p, p.neighbours));
                _particles.ForEach(p => p.Position += DeltaPosition(p));
                ConfineParticlesInBox(_particles, bounds);
            }
            
            _particles.ForEach(p => p.velocity = (p.Position - p.startPosition) / timeStep);
            //_particles.ForEach(p => p.Position = p.newPosition);
            
            timeAvgAll.Add(timerAll.Time);
            timeAll = timeAvgAll.Average() * 1000f;
            
            /*
            int sumNumNeighbours = 0;
            float sumDensity = 0f;
            foreach (var particle in _particles)
            {
                sumNumNeighbours += particle.neighbours.Count;
                sumDensity += DensityAt(particle.Position);
            }

            if (_particles.Count > 0)
            {
                Debug.Log("avg neighbours: " + sumNumNeighbours / _particles.Count);
                Debug.Log("avg density: " + sumDensity / _particles.Count);
            }*/
        }


        private void ApplyExternalForcesTo(LiquidParticle particle, float timeStep)
        {
            particle.velocity += Vector2.down * timeStep * gravity;
        }

        // Calculate the change in position for given particle
        // Equation 12 in Müller et al. 2003
        private Vector2 DeltaPosition(LiquidParticle particle)
        {
            Vector2 sum = Vector2.zero;
            foreach (var otherParticle in particle.neighbours)
            {
                Vector2 gradient = _smoothing.SpikyGradient(particle.Position - otherParticle.Position);
                sum += (particle.ScalingFactor + otherParticle.ScalingFactor) * gradient;
            }
            
            return 1f/restDensity * sum;
        }


    

        // 
        // Scaling factor λ for given particle
        // Equation 11 in Müller et al. 2003
        private float ScalingFactor(LiquidParticle particle, List<LiquidParticle> neighbours)
        {
            float densityConstraint =  DensityAt(particle.Position, neighbours) / restDensity - 1f;
            float sum = 0f;
            foreach (var otherParticle in neighbours)
            {
                sum += GradientOfConstraint(particle, otherParticle).sqrMagnitude;
            }
            
            return -densityConstraint /(sum + relaxationParameter);
        }
        
        
        private float DensityAt(Vector2 position, List<LiquidParticle> nearbyParticles = null)
        {
            if (nearbyParticles == null) nearbyParticles = GetLiquidParticlesInCircle(position, interactionDistance);
            
            float density = 0f;
            foreach (var particle in nearbyParticles)
            {
                float distance = (position - particle.Position).magnitude;
                density += _smoothing.Poly(distance) * particleMass;
            }
            return density;
        }
     
        
        // Gradient of constraint function for particle i with respect to particle k
        // Equation 8 in Müller et al. 2003
        private Vector2 GradientOfConstraint(LiquidParticle i, LiquidParticle k)
       {
            Vector2 result = Vector2.zero;

            if (i == k)
            {
                foreach (LiquidParticle j in i.neighbours)
                {
                    result += _smoothing.SpikyGradient(i.Position - j.Position);
                }
                return result / restDensity;
            }
            else
            {
                return -_smoothing.SpikyGradient( i.Position - k.Position) / restDensity;
            }
            
       }
        
        
        private List<LiquidParticle> NeighboursOf(LiquidParticle p) => GetLiquidParticlesInCircle(p.Position, interactionDistance);
        

     


        void ConfineParticlesInBox(List<LiquidParticle> particles, Rect boxBounds)
        {
            foreach (var particle in particles)
            {
                if (boxBounds.Contains(particle.Position)) continue;
                particle.Position = new Vector2(Mathf.Clamp(particle.Position.x, boxBounds.xMin, boxBounds.xMax),
                    Mathf.Clamp(particle.Position.y, boxBounds.yMin, boxBounds.yMax));
            }
        }
        
        void UpdateDensityTexture()
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    densityTexture.SetPixel(i, j, Color.red);
                }
            }
            densityTexture.Apply();
        }
        
        #endregion
    }
}
