using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FingerFollow : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Transform fingerImageTransform;
    [SerializeField] private bool pressedFlag;
    [SerializeField] private float pressedScale;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 canvasScale;
    [SerializeField] private Vector2 mousePos;
    [SerializeField] private Camera cam;
    [SerializeField] private Vector2 targetAnchroedPos;
    [SerializeField] private float smoothTime;
    [SerializeField] private float targetFingerScale;
    [SerializeField] private float fingerScaleSmothTime;
    private void Start(){
        cam = Camera.main;
        canvas =  GetComponentInParent<Canvas>();
        canvasScale = canvas.GetComponent<RectTransform>().sizeDelta;
        rectTransform = GetComponent<RectTransform>();
        targetFingerScale = 1f;
    }
    private void Update(){
        mousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        targetAnchroedPos = new Vector2(mousePos.x*canvasScale.x,mousePos.y*canvasScale.y);
        Vector2 refVelocity = new Vector2();
        rectTransform.anchoredPosition  = Vector2.SmoothDamp(rectTransform.anchoredPosition,targetAnchroedPos,ref refVelocity,smoothTime);

        if(Input.GetMouseButtonDown(0)){
            targetFingerScale = pressedScale;
        }
        else if(Input.GetMouseButtonUp(0)){
            targetFingerScale = 1f;
        }
        Vector3 refVelocity3d = new Vector3();
        fingerImageTransform.transform.localScale = Vector3.SmoothDamp(fingerImageTransform.transform.localScale,targetFingerScale*Vector3.one,ref refVelocity3d,fingerScaleSmothTime);
    }

    public void OpenFingerImage(){
       // fingerImageTransform.GetComponent<Image>().enabled = true;
    }

    public void CloseFingerImage(){
       // fingerImageTransform.GetComponent<Image>().enabled = false;
    }
}
