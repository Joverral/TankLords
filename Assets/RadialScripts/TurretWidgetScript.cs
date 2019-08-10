using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class TurretWidgetScript : MonoBehaviour {

    const int kLeftMouse = -1;
    const int kRightMouse = -2;
    const int kMiddleMouse = -3;

    [SerializeField]
    Image angleImage;

    [SerializeField]
    LayerMask terrainLayer;

    [SerializeField]
    GameObject turretArcCanvas;

    [SerializeField]
    private LineRenderer lineRenderer;


    private TurretScript selectedTurret;

    public void SetSelectedTurret(TurretScript turret)
    {
        selectedTurret = turret;
        UpdateDisplay(selectedTurret.turretToRotateHorizontal.transform.forward);

        this.Show();
    }

    private void UpdateDisplay(Vector3 turretDirection)
    {
        SetFill(selectedTurret.CurrentDegreesRemaining);
        turretArcCanvas.transform.position = selectedTurret.turretToRotateHorizontal.transform.position;
        turretArcCanvas.transform.forward = turretDirection;
        turretArcCanvas.transform.Rotate(Vector3.up, -selectedTurret.CurrentDegreesRemaining);
    }

    void SetFill(float angleDegrees)
    {
        angleImage.fillAmount = 2 * angleDegrees / 360.0f;
    }


    // returns true if it handled the event
    public bool OnPointerClick(PointerEventData pointerData)
    {
        bool handledClick = false;

        // check /right/left click
        if (pointerData.pointerId == kRightMouse)
        {
             RaycastHit hitInfo;
             Ray ray = Camera.main.ScreenPointToRay(pointerData.position);

            if (Physics.Raycast(ray, out hitInfo, 1000.0f, terrainLayer))
            {
                this.selectedTurret.BeginRotateTowards(hitInfo.point);
                UpdateDisplay(this.selectedTurret.TargetDir);
            }

            handledClick = true;
        }

        return handledClick;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (selectedTurret != null && !this.selectedTurret.IsTurning)
        {
            RaycastHit hitInfo;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hitInfo, 1000.0f, terrainLayer))
            {
                Vector3 turretPos = this.selectedTurret.turretToRotateHorizontal.transform.position;
                // clamp the position to within the arc, then set it.
                this.transform.position = turretPos + selectedTurret.GetClampedDir(hitInfo.point, float.MaxValue);

                lineRenderer.SetPosition(0, this.transform.position);
                lineRenderer.SetPosition(1, turretPos);
            }
        }
	}

    public void Show()
    {
        turretArcCanvas.SetActive(true);
        this.lineRenderer.gameObject.SetActive(true);
    }

    public void Hide()
    {
        turretArcCanvas.SetActive(false);
        this.lineRenderer.gameObject.SetActive(false);
    }
}
