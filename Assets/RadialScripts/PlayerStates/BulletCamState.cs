using UnityEngine;
using System.Collections;

public class BulletCamState : MonoBehaviour, PlayerState
{
    [SerializeField]
    PlayerController playerController;

    [SerializeField]
    Transform CameraRig;

    [SerializeField]
    AnimationCurve TimeScalarCurve;

    [SerializeField]
    AnimationCurve TimeScaleApproachCurve;

    //TODO: move to PenetrationCamState?
    [SerializeField]
    Material shardMaterial;

    Camera prevCamera;
    
    ShellScript shellScript;
    Transform targetProjectile;

    public Transform TargetProjectile 
    {
        get { return targetProjectile; }
        set
        {
            targetProjectile = value;
            shellScript = value.GetComponent<ShellScript>();
        }
    }



    GameEventSystem gameEventSystem;

    public Material CutOutMaterial;

    public bool hasPenetratedBefore = false;

    public float originalFixedDeltaTime;

    ModelController modelController;
	// Use this for initialization
	void Start () {

        originalFixedDeltaTime = Time.fixedDeltaTime;

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        gameEventSystem.Subscribe(this.gameObject, GameEventType.BulletLifetimeOver, OnBulletLifeEnded);

        gameEventSystem.Subscribe(this.gameObject, GameEventType.SuccessfulArmorPenetration, OnSuccessfulArmorPenetration);
	}

    public void OnBulletLifeEnded(GameObject sender, System.Object data)
    {
        // Pop yourself
        this.playerController.PopCurrentState();
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }

   
    public void OnSuccessfulArmorPenetration(GameObject sender, System.Object data)
    {
        if (!hasPenetratedBefore)
        {
            var node = sender.transform;

            // TODO: this is shit code.
       
            do
            {
                modelController = node.gameObject.GetComponent<ModelController>();
                node = node.parent;
            }
            while (modelController == null && node != null);

            modelController.HideRenderModel();
            modelController.SetGhostTransform(modelController.transform);
            modelController.ShowGhost();
            modelController.ShowInternals();

            this.CameraRig.position = RotatePointAroundPivot(this.CameraRig.position, TargetProjectile.position, Quaternion.Euler(0.0f, 90.0f, 0.0f));
        
           // this.CameraRig.transform.localPosition += Vector3.right * 5.0f;
            this.CameraRig.transform.LookAt(TargetProjectile);
            hasPenetratedBefore = true;

            Time.timeScale = 0.0075f * 0.5f;
            Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
            //Time.timeScale = 0.01f;

            var meshes = TargetProjectile.GetComponentsInChildren<MeshRenderer>();
            foreach(var mesh in meshes)
            {
                mesh.material = shardMaterial;
            }
        }
        
    }
	
	// Update is called once per frame
	void Update () {
	
         // follow the projectile at some distance
	}

    void FixedUpdate()
    {
        // This logic should probably go into bulletCamState...
        if (!hasPenetratedBefore)
        {
            const float kMaxOwnerNearDistance = 10.0f;
            Time.timeScale = Mathf.Lerp(0.01f * 0.5f, 1.0f * 0.5f, shellScript.DistanceTraveled / kMaxOwnerNearDistance);
            Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
            if (Time.timeScale == 1.0f)
            {
                RaycastHit hitInfo;
                var rigidBody = shellScript.GetComponent<Rigidbody>();
                const float lookAheadDistance = 100.0f;
                if (Physics.Raycast(shellScript.transform.position, rigidBody.velocity, out hitInfo, lookAheadDistance))
                {
                    Time.timeScale = TimeScaleApproachCurve.Evaluate(1.0f - hitInfo.distance / lookAheadDistance);
                 //   Debug.Log("PostCurve newTimeScale: " + Time.timeScale);
                }
            }
        }
    }

    public void OnConfirm()
    {
      //  throw new System.NotImplementedException();
    }

    public void OnCancel()
    {
    //    throw new System.NotImplementedException();
    }

    public void OnScreenPointerUp(UnityEngine.EventSystems.PointerEventData pointerData)
    {
  //      throw new System.NotImplementedException();
    }

    public void OnScreenClick(UnityEngine.EventSystems.PointerEventData pointerData)
    {
//        throw new System.NotImplementedException();
    }

    public void OnScreenPointerDown(UnityEngine.EventSystems.PointerEventData pointerData)
    {
 //       throw new System.NotImplementedException();
    }

    public void OnScreenDrag(UnityEngine.EventSystems.PointerEventData pointerData)
    {
    //    throw new System.NotImplementedException();
    }

    public void OnMove(UnityEngine.EventSystems.PointerEventData axisData)
    {
       // throw new System.NotImplementedException();
    }

    public void OnObjectSelected(GameObject go)
    {
//throw new System.NotImplementedException();
    }

    public void OnEnter()
    {
        hasPenetratedBefore = false;

        
        
        Time.timeScale = 0.01f * 0.5f;
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

        prevCamera = Camera.main;
        prevCamera.enabled = false;
        this.CameraRig.transform.position = playerController.selectedGameObject.transform.position + Vector3.up * 3.0f;
        this.CameraRig.transform.rotation = TargetProjectile.rotation;
        var autoCam = this.CameraRig.GetComponent<UnityStandardAssets.Cameras.AutoCam>();
        autoCam.SetTarget(this.TargetProjectile);

        var bulletCams = this.CameraRig.GetComponentsInChildren<Camera>();
        for (int i = 0; i < bulletCams.Length; ++i)
        {
            bulletCams[i].enabled = true;
        }
        
    }

    public void OnLeave()
    {
        prevCamera.enabled = true;

        var bulletCams = this.CameraRig.GetComponentsInChildren<Camera>();
        for(int i = 0; i < bulletCams.Length; ++i)
        {
            bulletCams[i].enabled = false;
        }
       
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        if (modelController != null)
        {
            modelController.HideGhost();
            modelController.HideInternals();
            modelController.ShowRenderModel();
        }
    }

    public void UpdateButtons()
    {
    //    throw new System.NotImplementedException();
    }
}
