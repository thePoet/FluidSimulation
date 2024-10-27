using FluidSimulation;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{
    public class Demo : MonoBehaviour
    {
        private Simulation _simulation;
        private bool _isPaused;
        
        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        
        void Awake()
        {
            SetMaxFrameRate(60);
            _isPaused = false;
            _simulation = FindObjectOfType<Simulation>();
        }


        void Update()
        {
            HandleUserInput();
        }
        
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void HandleUserInput()
        {
            if (Input.GetKeyDown(KeyCode.C)) _simulation.Clear();
            if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused;
        }
        
        private void SetMaxFrameRate(int frameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        }
        #endregion
    }
}