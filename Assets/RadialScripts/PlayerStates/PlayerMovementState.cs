using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerMovementState : MonoBehaviour, PlayerState
{
    [SerializeField]
    PlayerController playerController;

    [SerializeField]
    RangeForMoveable rangeCircle;
    [SerializeField]
    TurningRadiusForMoveable turnRadiusCircles;
    [SerializeField]
    ControlWidgetScript controlWidget;

    [SerializeField]
    Camera movementCamera;

    private Quaternion startingOrientation;

    public float sensitivityX = 25F;
    public float sensitivityY = 15F;
    public float sensitivityZ = 30000f;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    public float maxAltitude = 200.0f;
    public float minAltitude = 1.0f;

    public float maxAltitudeRate = 300.0f;
    public float minAltitudeRate = .25f;


    float targetAltitude;

    float rotationY = 0F;

    float mouseHorizontalOverride = 0.0f;
    float mouseVerticalOverride = 0.0f;

    //todo put these someplace central...why they are defined by Unity anyway, I don't know
    const int kLeftMouse = -1;
    const int kRightMouse = -2;
    const int kMiddleMouse = -3;

    public float AltitudeAsTvalue()
    {
        // todo:  cache?
        return Mathf.InverseLerp(minAltitude, maxAltitude, movementCamera.transform.position.y);
    }

    // Use this for initialization
    void Start()
    {
        this.startingOrientation = movementCamera.transform.localRotation;
        this.rotationY = -movementCamera.transform.localEulerAngles.y;
        targetAltitude = movementCamera.transform.position.y;
    }

    // Update is called once per frame

    //TODO:  Add a scalar, expose scalar to properties
    void Update()
    {

        var directionVector = new Vector3(Input.GetAxis("Horizontal"),
                                          0,
                                          Input.GetAxis("Vertical"));

        if (!MyMath.IsNearZero(mouseHorizontalOverride) || !MyMath.IsNearZero(mouseVerticalOverride))
        {
            directionVector.x = mouseHorizontalOverride;
            directionVector.z = mouseVerticalOverride;
        }

        if (directionVector != Vector3.zero)
        {
            // Get the length of the directon vector and then normalize it
            // Dividing by the length is cheaper than normalizing when we already have the length anyway
            var directionLength = directionVector.magnitude;
            directionVector = directionVector / directionLength;

            // Make sure the length is no bigger than 1
            directionLength = Mathf.Min(1, directionLength);

            // Make the input vector more sensitive towards the extremes and less sensitive in the middle
            // This makes it easier to control slow speeds when using analog sticks
            directionLength = directionLength * directionLength;

            // Multiply the normalized direction vector by the modified length
            directionVector = directionVector * directionLength;

            Quaternion flatRot = Quaternion.Euler(0, movementCamera.transform.rotation.eulerAngles.y, 0);


            float t = Mathf.InverseLerp(minAltitude, maxAltitude, movementCamera.transform.position.y);

            movementCamera.transform.position = movementCamera.transform.position + flatRot * directionVector * t * 2.0f;
        }

        if (Input.GetAxis("Zoom") != 0)
        {
            float zoom = Input.GetAxis("Zoom") * sensitivityZ * Time.deltaTime;
            float t = Mathf.Abs(movementCamera.transform.position.y) / maxAltitude;
            targetAltitude = movementCamera.transform.position.y - zoom * t;
            targetAltitude = Mathf.Clamp(targetAltitude, minAltitude, maxAltitude);
           

            //			transform.position = Vector3.MoveTowards(transform.position, 
            //											   transform.position - new Vector3(0, Input.GetAxis("Zoom")*sensitivityZ, 0),
            //												1000F);
        }

        //TODO:  Adjust the height of the camera to be slightly above the heightmap at the current location

        //Mouse look
        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            float rotationX = movementCamera.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            Quaternion destRotation = Quaternion.Euler(-rotationY, rotationX, 0.0f);
            movementCamera.transform.localRotation = Quaternion.Slerp(movementCamera.transform.localRotation, destRotation, Time.deltaTime * 10.0f);
            //transform.localEulerAngles = new Vector3();
        }

        if (Input.GetButton("ResetView"))
        {
            movementCamera.transform.localRotation = this.startingOrientation;
        }


        if(targetAltitude != movementCamera.transform.position.y)
        {
            float altT = Mathf.Pow(AltitudeAsTvalue(), 2.0f);

            Vector3 camPos = movementCamera.transform.position; // just to shorten things
            float altitudeRate = Mathf.Lerp(minAltitude, maxAltitudeRate, altT);
            movementCamera.transform.position =
                new Vector3(camPos.x,
                            Mathf.MoveTowards(camPos.y, targetAltitude, altitudeRate * Time.deltaTime),
                            camPos.z);
        }
    }

    public void OnConfirm()
    {
        playerController.selectedGameObject.GetComponent<TileMoveable>().BeginMove();
    }

    public void OnCancel()
    {
        Debug.LogError("Trying to cancel move mode?");
    }

    public void OnScreenPointerUp(PointerEventData pointerData)
    {
        controlWidget.OnPointerUp(pointerData);
    }

    public void OnScreenClick(PointerEventData pointerData)
    {
        RaycastHit hitInfo;
        Ray ray = Camera.main.ScreenPointToRay(pointerData.position);


        if (Physics.Raycast(ray, out hitInfo, 1000.0f, playerController.unitLayer) && pointerData.pointerId == kLeftMouse)
        {
            playerController.SelectObject(hitInfo.transform.gameObject);
            TileMoveable moveable = playerController.selectedGameObject.GetComponent<TileMoveable>();
            controlWidget.Select(moveable);


            rangeCircle.UseForMoveable(moveable);
            turnRadiusCircles.UseForMoveable(moveable);
        }
        else
        {
            controlWidget.OnPointerClick(pointerData);
        }
    }

    public void OnScreenPointerDown(PointerEventData pointerData)
    {
        controlWidget.OnPointerDown(pointerData);
    }

    public void OnScreenDrag(PointerEventData pointerData)
    {
        controlWidget.OnPointerDrag(pointerData);
    }

    public void OnObjectSelected(GameObject go)
    {

    }

    public void OnEnter()
    {
        rangeCircle.gameObject.SetActive(true);
        turnRadiusCircles.gameObject.SetActive(true);
        controlWidget.gameObject.SetActive(true);
    }

    public void OnLeave()
    {
        rangeCircle.gameObject.SetActive(false);
        turnRadiusCircles.gameObject.SetActive(false);
        controlWidget.gameObject.SetActive(false);
    }

    public void OnMove(PointerEventData axisData)
    {

    }

    public void UpdateButtons()
    {
        TileMoveable moveable = playerController.selectedGameObject.GetComponent<TileMoveable>();
        playerController.confirmButton.gameObject.SetActive(moveable.CurrentPath != null && moveable.CurrentMoveDistance != 0.0f);
        playerController.cancelButton.gameObject.SetActive(false);
    }
}
