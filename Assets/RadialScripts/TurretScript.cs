using UnityEngine;
using System.Collections;

public class TurretScript : MonoBehaviour {

    [SerializeField]
    float HorizontalTraverseDegreesPerSecond = 44.0f;
    [SerializeField]
    float VerticalTraverseDegreesPerSecond = 16.0f;


    [SerializeField]
    float MaxVerticalAngleDegrees = 20.0f;
    [SerializeField]
    float MinVerticalAngleDegrees = -10.0f;

    public float MaxAngleInDegrees = 100;
    public float CurrentDegreesRemaining = 100;
    public Transform turretToRotateHorizontal;
    public Transform turretToRotateVertical;
    public Transform AimRayOrigin;


    GameEventSystem gameEventSystem;
    float targetAngle;
    bool isTurning = false;
    Vector3 finalDir;


    Quaternion targetVerticalRot;
    Quaternion targetHorzRot;
    float targetVerticalAngle;
    float targetHorzAngle;

    Collider[] selfColliders;

    public bool IsTurning { get { return isTurning; } }
    public Vector3 TargetDir { get { return finalDir; } }

	// Use this for initialization
	void Start () {

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");

        // I shouldn't do this here...
        gameEventSystem = go.GetComponent<GameEventSystem>();
        gameEventSystem.Subscribe(this.gameObject, GameEventType.TargetOrientationChanged, OnTargetChange);

        // TODO:  I shouldn't need these if I keep everything in local rotation
        gameEventSystem.Subscribe(this.gameObject, GameEventType.AttackModeLeft, OnLeavingAttackMode);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.AttackModeEntered, OnLeavingAttackMode);

        ResetTargetRotations();

        selfColliders = this.GetComponentsInChildren<Collider>();
	}

    private void ResetTargetRotations()
    {
        targetHorzAngle = turretToRotateHorizontal.localRotation.eulerAngles.y;
        targetVerticalAngle = turretToRotateVertical.localRotation.eulerAngles.x;
    }

    private Vector3 FindHitPoint(Ray ray)
    {
        const float defaultRange = 1000.0f;
        RaycastHit hit;
        Vector3 hitPoint;

        // TODO:  Not sure the best method here
        // my problem is that I'm raycasting from the camera
        // and hitting my own tank, which is obviously not good.
        // I could try to change the layer of all the colliders.

        for (int i = 0; i < selfColliders.Length; ++i)
        {
            selfColliders[i].enabled = false;
        }

        if (Physics.Raycast(ray, out hit))
        {
            hitPoint = hit.point;
        }
        else
        {
            hitPoint = ray.GetPoint(defaultRange);
        }

        for (int i = 0; i < selfColliders.Length; ++i)
        {
            selfColliders[i].enabled = true;
        }

        return hitPoint;
    }

    public void OnLeavingAttackMode(GameObject sender, System.Object data)
    {
        ResetTargetRotations();
    }

    public void OnTargetChange(GameObject sender, System.Object data)
    {// todo:  capture the ray, move towadrs it on lateUpdate
        if (this.gameObject != sender)
            return;

        var ray = (Ray)data; 
        // Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f));

        Vector3 hitPoint = FindHitPoint(ray);
        Debug.DrawLine(ray.origin, hitPoint, Color.red);

        var muzzlePos = this.GetComponent<WeaponScript>().MuzzlePosition;
        var turretMuzzleOffset = this.turretToRotateHorizontal.transform.position - muzzlePos.position;
        var adjustedHitPoint = hitPoint + turretMuzzleOffset;

        Debug.DrawLine(this.turretToRotateHorizontal.transform.position, adjustedHitPoint, Color.blue);

        var targetVec = adjustedHitPoint - this.turretToRotateHorizontal.position;
        targetVec = Vector3.ProjectOnPlane(targetVec, turretToRotateHorizontal.up);

        // TODO:  I haven't a clue why I need to do this...
        // I should be able to inverse transform the vector and get the angle between...
        Vector3 clonePos = turretToRotateHorizontal.localPosition;
        Quaternion cloneRot = turretToRotateHorizontal.localRotation;

        turretToRotateHorizontal.forward = targetVec;
        targetHorzAngle = turretToRotateHorizontal.localEulerAngles.y;
        turretToRotateHorizontal.localPosition = clonePos;
        turretToRotateHorizontal.localRotation = cloneRot;

       // targetHorzAngle = Vector3.Angle(turretToRotateHorizontal.forward, targetVec); //Quaternion.LookRotation(targetVec).eulerAngles.y;

        Debug.Log("targetHorzAngle: " + targetHorzAngle);

        turretMuzzleOffset = this.turretToRotateVertical.transform.position - muzzlePos.position;
        adjustedHitPoint = hitPoint + turretMuzzleOffset;
        targetVec = adjustedHitPoint - this.turretToRotateVertical.transform.position;

        targetVerticalAngle = ClampVertical(Quaternion.LookRotation(targetVec).eulerAngles.x);

        //targetVerticalRot = Quaternion.Euler(
        //    ClampVertical(Quaternion.LookRotation(targetVec).eulerAngles.x), 0.0f, 0.0f);

        
        //targetHorzRot = Quaternion.Euler(0.0f, Quaternion.LookRotation(targetVec).eulerAngles.y, 0.0f);

        //turretMuzzleOffset = this.turretToRotateVertical.transform.position - muzzlePos.position;
        //adjustedHitPoint = hitPoint + turretMuzzleOffset;
        //targetVec = adjustedHitPoint - this.turretToRotateVertical.transform.position;
        //targetVerticalRot = Quaternion.Euler(
        //    ClampVertical(Quaternion.LookRotation(targetVec).eulerAngles.x), 0.0f, 0.0f);

        //Debug.Log("EulerX = " + targetVerticalRot.eulerAngles.x);
    }
	
    float ClampVertical(float eulerXAngleDegrees)
    {
        // I'll be honest, I don't know wtf the euler angles are doing here.
        if (eulerXAngleDegrees > 180.0f)
        {
            return Mathf.Max((eulerXAngleDegrees - 360.0f), -MaxVerticalAngleDegrees);
        }
        else
        {
            return Mathf.Min(eulerXAngleDegrees, -MinVerticalAngleDegrees);
        }
        
    }

	// Update is called once per frame
	void Update () {


        // TODO:  Only active objects should turn....
        bool turretRotationChanged = false;

        if (turretToRotateHorizontal.localRotation.eulerAngles.y != targetHorzAngle)
        {
            float horzAngle = HorizontalTraverseDegreesPerSecond * Time.deltaTime;
            Vector3 localEuler = turretToRotateHorizontal.localRotation.eulerAngles;
            turretToRotateHorizontal.localRotation = Quaternion.Euler( 
                    localEuler.x,
                    Mathf.MoveTowardsAngle(localEuler.y, targetHorzAngle, horzAngle),
                    localEuler.z);
                
                //Quaternion.RotateTowards(turretToRotateHorizontal.localRotation, targetHorzRot, horzAngle);

            turretRotationChanged = true;
        }
            
        if (turretToRotateVertical.localRotation.eulerAngles.x != targetVerticalAngle)
        {

            float vertAngle = VerticalTraverseDegreesPerSecond * Time.deltaTime;
            Vector3 localEuler = turretToRotateVertical.localRotation.eulerAngles;
            turretToRotateVertical.localRotation = Quaternion.Euler(
                   Mathf.MoveTowardsAngle(localEuler.x, targetVerticalAngle, vertAngle),
                   localEuler.y,
                   localEuler.z);
            //turretToRotateVertical.localRotation =
            //    Quaternion.RotateTowards(turretToRotateVertical.localRotation, targetVerticalRot, vertAngle);

            turretRotationChanged = true;
        }

         
        if (turretRotationChanged)
        {
            var muzzlePos = this.GetComponent<WeaponScript>().MuzzlePosition;
            var hitPoint = FindHitPoint(new Ray(muzzlePos.position, muzzlePos.forward));

            // Let the UI know where the tank is actually pointing
            gameEventSystem.RaiseEvent(GameEventType.TurretOrientationChanged, this.gameObject, hitPoint);
        }
	}

    public Vector3 GetClampedDir(Vector3 lookAtPoint, float maxLength)
    {
        float notUsedAngle;
        return GetClampedDir(lookAtPoint, maxLength, out notUsedAngle);
    }

    public Vector3 GetClampedDir(Vector3 lookAtPoint, float maxLength, out float clampedAngleDegrees)
    {
        Vector3 xzVec = new Vector3(lookAtPoint.x - turretToRotateHorizontal.transform.position.x,
                                    0.0f,
                                    lookAtPoint.z - turretToRotateHorizontal.transform.position.z);

        float angle = Vector3.Angle(turretToRotateHorizontal.forward, xzVec);

        clampedAngleDegrees = Mathf.Min(Mathf.Abs(angle), CurrentDegreesRemaining);
      
        if (angle < 0.0f)
        {
            clampedAngleDegrees *= -1.0f;
        }

        return Vector3.RotateTowards( turretToRotateHorizontal.forward, 
                                     xzVec,
                                     Mathf.Deg2Rad * clampedAngleDegrees, maxLength);
    }

    public void BeginRotateTowards(Vector3 lookAtPoint)
    {
        if (!isTurning)
        {
            isTurning = true;
            finalDir = GetClampedDir(lookAtPoint, 1.0f, out targetAngle).normalized; // 
            CurrentDegreesRemaining -= targetAngle;  // TODO clamp to ensure never go below zero
        }
    }
}
