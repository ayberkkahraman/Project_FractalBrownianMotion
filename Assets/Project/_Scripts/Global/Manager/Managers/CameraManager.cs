using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project._Scripts.Global.Manager.Managers
{
    public class CameraManager : MonoBehaviour
    {
        #region Cameras
        public CinemachineVirtualCamera CurrentCamera;
        public CinemachineComponentBase ComponentBase;
        #endregion

        #region Initialize
        private void Awake()
        {
            ComponentBase = CurrentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
        }
        #endregion
        #region Camera
        /// <summary>
        /// Updates the position of the camera target
        /// </summary>
        /// <param name="pos"></param>
        public void UpdateCam(Vector3 pos)
        {
            CurrentCamera.Follow.position = pos;
        }

        public void UpdateDistance(float distance)
        {
            if (ComponentBase is CinemachineFramingTransposer transposer)
            {
                transposer.m_CameraDistance = distance;
            }
        }
        #endregion
    }
}