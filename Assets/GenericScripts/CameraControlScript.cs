using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraControlScript : MonoBehaviour {
	
    interface CameraController
    {
        void OnUpdate();
    }

    //class DefaultCameraState : CameraState
    //{
    //    public void OnUpdate()
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}

	private Quaternion startingOrientation;
	
	public float sensitivityX = 25F;
	public float sensitivityY = 15F;
	public float sensitivityZ = 300f;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;
	
	public float maxAltitude = 200.0f;
	public float minAltitude = 1.0f;
	
	float rotationY = 0F;
	
	float mouseHorizontalOverride = 0.0f;
	float mouseVerticalOverride = 0.0f;
	
	public float AltitudeAsTvalue()
    {
        // todo:  cache?
        return Mathf.InverseLerp(minAltitude, maxAltitude, transform.position.y);
    }
	
	// Use this for initialization
	void Start () {
		this.startingOrientation = this.transform.localRotation;
		this.rotationY = -this.transform.localEulerAngles.y;
	}
	
	// Update is called once per frame
	
	//TODO:  Add a scalar, expose scalar to properties
	void Update () 
	{
		
		var directionVector = new Vector3(Input.GetAxis("Horizontal"),
										  0,
									      Input.GetAxis("Vertical"));
		
		if ( !MyMath.IsNearZero(mouseHorizontalOverride) || !MyMath.IsNearZero(mouseVerticalOverride) )
		{
			directionVector.x = mouseHorizontalOverride;
			directionVector.z = mouseVerticalOverride;
		}
		
		if (directionVector != Vector3.zero) {
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
			
			Quaternion flatRot = Quaternion.Euler(0,transform.rotation.eulerAngles.y,0);


            float t = Mathf.InverseLerp(minAltitude, maxAltitude, transform.position.y);

			transform.position = transform.position + flatRot* directionVector * t * 2.0f;
		}
		
		if(Input.GetAxis("Zoom") != 0)
		{
			float zoom = Input.GetAxis("Zoom")* sensitivityZ / 4.0f;
			float t = Mathf.Abs(transform.position.y) / maxAltitude;
			Vector3 newPosition = transform.position - 
									new Vector3(0, Mathf.Lerp(zoom, zoom*sensitivityZ * Time.deltaTime, t), 0);
			newPosition.y = Mathf.Clamp(newPosition.y, minAltitude, maxAltitude);
			transform.position = Vector3.Lerp( transform.position, 
											   newPosition,
												Time.deltaTime*10.0f);
			
//			transform.position = Vector3.MoveTowards(transform.position, 
//											   transform.position - new Vector3(0, Input.GetAxis("Zoom")*sensitivityZ, 0),
//												1000F);
		}
		
		//TODO:  Adjust the height of the camera to be slightly above the heightmap at the current location
		
		//Mouse look
        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
		{
			float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			Quaternion destRotation = Quaternion.Euler(-rotationY, rotationX, 0.0f);
			transform.localRotation = Quaternion.Lerp(this.transform.localRotation, destRotation, Time.deltaTime*10.0f);
			//transform.localEulerAngles = new Vector3();
		}
		
		if(Input.GetButton("ResetView"))
		{
			this.transform.localRotation = this.startingOrientation;
		}
	}

}
