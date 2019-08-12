using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UnitBannerScript : MonoBehaviour {

    [SerializeField] 
    Image BannerImage;
    [SerializeField] 
    Transform UnitTransform;
    [SerializeField] 
    Camera TargetCamera;

    [SerializeField] 
    float MinSize;
    [SerializeField] 
    float MaxSize;

    CameraControlScript cameraControlScript;
	// Use this for initialization
	void Start () {
        cameraControlScript = TargetCamera.GetComponent<CameraControlScript>();
	}
	
	// Update is called once per frame
	void Update () {
	    //TODO:  Move this to be event driven

        if (BannerImage != null && UnitTransform != null && TargetCamera != null)
        {
            float t = cameraControlScript.AltitudeAsTvalue();
            //BannerImage.transform.LookAt(TargetCamera.transform.position);
            // transform the unit transform to screen space
            // update the banner position
            Vector3 unitScreenPos = TargetCamera.WorldToScreenPoint(this.UnitTransform.position);
            //unitScreenPos = new Vector3(Mathf.Clamp(unitScreenPos.x, 0 + (windowWidth / 2), Screen.width - (windowWidth / 2)),
            //                           Mathf.Clamp(unitScreenPos.y, 50, Screen.height),
            //                           unitScreenPos.z);
            this.BannerImage.rectTransform.position = unitScreenPos + Vector3.up * 100.0f * (1.0f - t);
            
            float left = this.BannerImage.rectTransform.rect.x;
            float top = this.BannerImage.rectTransform.rect.y;
            //this.BannerImage.rectTransform.rect = new Rect(left, top, Mathf.Lerp(MinSize, MaxSize, t),  Mathf.Lerp(MinSize, MaxSize, t));
            this.BannerImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(MinSize, MaxSize, t));
        }
	}
}
