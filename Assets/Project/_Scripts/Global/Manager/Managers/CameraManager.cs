using Cinemachine;
using UnityEngine;

namespace Project._Scripts.Global.Manager.Managers
{
    public class CameraManager : MonoBehaviour
    {
        #region Cameras
        public CinemachineVirtualCamera CurrentCamera;
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
        #endregion
    }
}