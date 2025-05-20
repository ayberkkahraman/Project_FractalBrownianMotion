using System;
using Cinemachine;
using UnityEngine;

namespace Project._Scripts.Global.Manager.Managers
{
    public class CameraManager : MonoBehaviour
    {
        #region Components
        private CinemachineComponentBase _componentBase;
        #endregion

        #region Fields
        public CinemachineVirtualCamera CurrentCamera;
        
        [Range(1f, 5f)]public float RotationSpeed = 3f;
        private float _currentAngle;
        private Vector3 _currentEulerRotation;
        #endregion

        #region Unity Functions
        private void Awake()
        {
            _currentEulerRotation = CurrentCamera.transform.eulerAngles;
            _componentBase = CurrentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
        }

        private void LateUpdate()
        {
            _currentAngle += Time.deltaTime * RotationSpeed;
            _currentEulerRotation.y = _currentAngle;
            CurrentCamera.transform.localEulerAngles = _currentEulerRotation;
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

        /// <summary>
        /// Updates the distance of the camera
        /// </summary>
        /// <param name="distance"></param>
        public void UpdateDistance(float distance)
        {
            if (_componentBase is CinemachineFramingTransposer framingTransposer)
            {
                framingTransposer.m_CameraDistance = distance;
            }
        }
        #endregion
    }
}