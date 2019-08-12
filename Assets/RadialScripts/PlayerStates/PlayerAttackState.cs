using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.CrossPlatformInput;
using System;

[Serializable]
public class MyMouseLook
{
    public float sensitivityX = 25F;
    public float sensitivityY = 15F;
    public float smoothTime = 15f;
    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    float rotationY = 0F;



   // private Quaternion m_CharacterTargetRot;
   // private Quaternion m_CameraTargetRot;


    //public void Init(Transform character, Transform camera)
    //{
    //   // m_CharacterTargetRot = character.localRotation;
    //    m_CameraTargetRot = camera.localRotation;
    //}

    public void LookRotation(Transform character, Transform camera, float adjustedSensitivity)
    {
        float rotationX = camera.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX * adjustedSensitivity;

        rotationY += Input.GetAxis("Mouse Y") * sensitivityY * adjustedSensitivity;
        rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

        Quaternion destRotation = Quaternion.Euler(-rotationY, rotationX, 0.0f);
        camera.transform.localRotation =
            Quaternion.Slerp(camera.transform.localRotation, destRotation, Time.deltaTime * smoothTime);
    }
}

public class PlayerAttackState : MonoBehaviour, PlayerState
{
    //TODO:  Move this somewhere unified
    const int kLeftMouse = -1;
    const int kRightMouse = -2;
    const int kMiddleMouse = -3;

    [SerializeField]
    PlayerController playerController;

    [SerializeField]
    Image CrossHairImage;

    [SerializeField]
    Image CrossHairImage_Actual;

    Vector3 originalCamPos;
    Quaternion originalCamRot;
    float originalFoV;

    AttackerScript attacker;


    GameEventSystem gameEventSystem;

    public MyMouseLook MouseLook = new MyMouseLook();
    public Camera lookCamera;

    public float sensitivityZ = 300f;
    public float minFoV = 30F;
    public float maxFoV = 60F;

    [SerializeField]
    float ZoomRate = 5.0f;

    float targetFoV;


    [SerializeField]
    BulletCamState bulletCamState;

    //public float sensitivityX = 25F;
    //public float sensitivityY = 15F;

    //    public float minimumY = -60F;
    //public float maximumY = 60F;
    //float rotationY = 0F;

	// Use this for initialization
	void Start () {

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        gameEventSystem.Subscribe(this.gameObject, GameEventType.TurretOrientationChanged, OnTurretChange);

        
	}
	
    public void OnTurretChange(GameObject sender, System.Object data)
    {
        if (sender == playerController.selectedGameObject)
        {
            //TODO:  Still kludgey 
            var aimVec = Camera.main.WorldToScreenPoint((Vector3)data);
            aimVec.z = -1;
            CrossHairImage_Actual.rectTransform.position = aimVec;
        }
    }

	// Update is called once per frame
	void Update ()
    {
        if (attacker != null && lookCamera != null)
        {
            // TODO:  Move the turret to the mouselook location...slowly
            MouseLook.LookRotation(transform, lookCamera.transform, lookCamera.fieldOfView / maxFoV);

            //TODO:  Kind of kludgey

            var ray = lookCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f));
            gameEventSystem.RaiseEvent(GameEventType.TargetOrientationChanged, attacker.gameObject, ray);

            if (Input.GetAxis("Zoom") != 0)
            {
                float fov = targetFoV - (Input.GetAxis("Zoom") * sensitivityZ * Time.deltaTime * (targetFoV / minFoV));
                targetFoV = Mathf.Clamp(fov, minFoV, maxFoV);
            }

            if(Input.GetButton("Fire1")) // todo move to down?
            {
                // Fire the shot
                // leave attack mode
                // enter shot cam mode
                
                // TODO: Accuracy on shot, perhaps pass in here.

                bulletCamState.TargetProjectile = attacker.FireWeapon();

                

                var bulletCollider = bulletCamState.TargetProjectile.GetComponent<Collider>();
                foreach(var attackerCollider in attacker.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(bulletCollider, attackerCollider);
                }
                
                playerController.ReplaceState(bulletCamState);
            }
            else if (Input.GetButton("Fire2"))
            {
                playerController.PopCurrentState();
            }
        }

        if(lookCamera.fieldOfView != targetFoV)
        {
            lookCamera.fieldOfView = Mathf.MoveTowards(lookCamera.fieldOfView, targetFoV, ZoomRate);
        }
	}
    public void OnConfirm()
    {
        //throw new System.NotImplementedException();
    }

    public void OnCancel()
    {
       // throw new System.NotImplementedException();
    }

    public void OnScreenPointerUp(PointerEventData pointerData)
    {
        //  throw new System.NotImplementedException();
    }

    public void OnScreenClick(PointerEventData pointerData)
    {
        //bool handledClick = playerController.turretWidget.OnPointerClick(pointerData);

        //if (!handledClick)
        //{
        //    // if the widget didn't handle the click, lets default to object selection
        //    // users can click on a new unit.
        //}
        // RaycastHit hitInfo;
        //Ray ray = Camera.main.ScreenPointToRay(pointerData.position);

        //if (Physics.Raycast(ray, out hitInfo, 1000.0f, playerController.unitLayer))
        //{
    }

    public void OnScreenPointerDown(PointerEventData pointerData)
    {
        if (pointerData.pointerId == kLeftMouse)
        {

        }
        else if (pointerData.pointerId == kRightMouse)
        {
            // leave this state.

            playerController.PopCurrentState();
        }
        //  throw new System.NotImplementedException();
    }

    public void OnScreenDrag(PointerEventData pointerData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnObjectSelected(GameObject go)
    {
        //throw new System.NotImplementedException();
    }

    public void OnMove(PointerEventData axisData)
    {

    }

    public void OnEnter()
    {
        if (playerController.selectedGameObject != null)
        {
            originalCamRot = Camera.main.transform.rotation;
            originalCamPos = Camera.main.transform.position;

            attacker = playerController.selectedGameObject.GetComponent<AttackerScript>();

            lookCamera = Camera.main;

            lookCamera.transform.position = attacker.CurrentWeapon.WeaponMount.position;
            lookCamera.transform.rotation = attacker.CurrentWeapon.WeaponMount.rotation;
           
            targetFoV = originalFoV = lookCamera.fieldOfView;
            //MouseLook.Init(transform, lookCamera.transform);
            CrossHairImage.gameObject.SetActive(true);
            CrossHairImage_Actual.gameObject.SetActive(true);

            Debug.Log("Locking mouse cursor");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

          //  gameEventSystem.RaiseEvent(GameEventType.AttackModeEntered, attacker.gameObject, null);
            
        }
    }

    public void OnLeave()
    {
        CrossHairImage.gameObject.SetActive(false);
        CrossHairImage_Actual.gameObject.SetActive(false);
        Camera.main.transform.position = originalCamPos;
        Camera.main.transform.rotation = originalCamRot;
        Camera.main.fieldOfView = originalFoV;

        Debug.Log("UNLOCKING mouse cursor");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // playerController.turretWidget.Hide();

        gameEventSystem.RaiseEvent(GameEventType.AttackModeLeft, attacker.gameObject, null);
    }

    public void UpdateButtons()
    {
        playerController.confirmButton.gameObject.SetActive(false);
        playerController.cancelButton.gameObject.SetActive(false);
        //throw new System.NotImplementedException();
    }
}
