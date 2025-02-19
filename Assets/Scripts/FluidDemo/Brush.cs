using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using static FluidDemo.SubstanceId;

namespace FluidDemo
{
    public class Brush : MonoBehaviour
    {
        public int particlesPerFrame = 1;
        public bool oneAtTime = false;
        public float brushRadius = 10f;
        public float maxSpeed = 10f;
        public Simulation simulation;
        public TextMeshPro text;
        
        private Vector2 _previousMousePosition;
        private SubstanceId _currentSubstanceId = SubstanceId.Water;
        private int _currentMode;
        
        private void Start()
        {
            simulation = FindObjectOfType<Simulation>();
            if (simulation == null) Debug.LogError("No simulation found in the scene.");
            SelectMode(1);
        }

        void Update()
        {
            if (simulation is null) return;
            
            if (LeftMouseButton && _currentMode <= 6)
            {
                CreateParticles();
            }
            if (LeftMouseButton && _currentMode == 7)
            {
                PushParticles();
            }
            if (LeftMouseButton && _currentMode == 8)
            {
                DeleteParticles();
            }
            if (LeftMouseButton && _currentMode == 9)
            {
                Smokify();
            }

            
            if (Input.GetKey(KeyCode.Alpha1)) SelectMode(1);
            if (Input.GetKey(KeyCode.Alpha2)) SelectMode(2);
            if (Input.GetKey(KeyCode.Alpha3)) SelectMode(3);
            if (Input.GetKey(KeyCode.Alpha4)) SelectMode(4);
            if (Input.GetKey(KeyCode.Alpha5)) SelectMode(5);
            if (Input.GetKey(KeyCode.Alpha6)) SelectMode(6);
            if (Input.GetKey(KeyCode.Alpha7)) SelectMode(7);
            if (Input.GetKey(KeyCode.Alpha8)) SelectMode(8);
            if (Input.GetKey(KeyCode.Alpha9)) SelectMode(9);


            _previousMousePosition = MousePosition;
        }

        private void PushParticles()
        {
            Vector2 deltaMousePosition = MousePosition - _previousMousePosition;
            Vector2 velocity = deltaMousePosition/Time.deltaTime;
        
            foreach (var id in ParticlesInBrush)
            {
                var particle = simulation.GetParticle(id);
                particle.Velocity = velocity;
                simulation.UpdateParticle(particle);
            }
        }
        
        private void DeleteParticles()
        {
            foreach (var id in ParticlesInBrush)
            {
                simulation.DestroyParticle(id);
            }
        }

        private bool CreateParticles()
        {
            int amount = particlesPerFrame;

            if (oneAtTime || _currentSubstanceId == SubstanceId.Rock )
            {
                amount = 1;
                if (!Input.GetMouseButtonDown(0)) return true;
            }
        
            for (int i=0; i < amount; i++)
            { 
                simulation.SpawnParticle(MousePosition + RandomOffset, Velocity, _currentSubstanceId);
            }

            return false;
        }
        
        private void Smokify()
        {
            var pib = ParticlesInBrush;
            if (pib.Length==0) return;
            int n = Random.Range(0, pib.Length - 1);
            var pos = simulation.GetParticle(pib[n]).Position;
            simulation.SpawnParticle(pos, Vector2.zero, SubstanceId.Smoke);
            simulation.DestroyParticle(pib[n]);
        }

        private string ModeSelectionText()
        {
            string[] texts = new string[]
            {
                "Water",
                "Air",
                "Smoke",
                "Rock",
                "Green Liquid",
                "Red Liquid",
                "Push",
                "Delete",
                "Turn to smoke"
            };

            string result = "";
            for (int i = 0; i < texts.Length; i++)
            {
                if (_currentMode == i+1) result += "<b>";
                result += "[" + (i + 1) + "] " + texts[i] + "   ";
                if (_currentMode == i+1) result += "</b>";
            }

            return result;
        }

        void SelectMode(int modeNumber)
        {
            _currentMode = modeNumber;
            if (_currentMode is >= 1 and <= 6) _currentSubstanceId = (SubstanceId)(_currentMode-1);
            text.text = ModeSelectionText();
        }

        private ParticleId[] ParticlesInBrush => simulation.ParticlesInsideCircle(MousePosition, 15f);
 
        bool LeftMouseButton => Input.GetMouseButton(0);

        bool RightMouseButton => Input.GetMouseButton(1);
       
        Vector2 RandomOffset => Random.insideUnitCircle * brushRadius;
        
        Vector2 Velocity => Vector2.down * maxSpeed;
        
        Vector2 MousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

    }
}
