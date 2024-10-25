using FluidSimulation;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;

namespace FluidDemo
{
    public class Demo : MonoBehaviour
    {

        #region ------------------------------------------- UNITY METHODS -----------------------------------------------
        void Awake()
        {
            SetMaxFrameRate(60);
        }


        void Update()
        {
        }
        
        #endregion


        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        private void SetMaxFrameRate(int frameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        }
        #endregion
    }
}