using Project._Scripts.Global.Manager.Core;
using Project._Scripts.Global.Manager.Managers;
using UnityEngine;
public interface ICamOwner
{
  public ICamOwner CamOwner { get; set; }
  public CameraManager CameraManager => ManagerCore.Instance.GetInstance<CameraManager>();
  public void UpdateCam(Vector3 pos) => CameraManager.UpdateCam(pos);
}
