

using UnityEngine;
using System.Collections;



public class ShellScript : MonoBehaviour {

    public float InitialVelocity = 800.0f;
    public float LifeSpan = 10.0f; // todo move to max range?
    public float Penetration = 50.0f; // in mm
    public AnimationCurve PenetrationDropoff;
    public float MaxRange = 2000.0f; // in meters
    public float IsolatedTimeScale = 1.0f; //0.5f;

    public GameObject Shrapnel;
    public GameObject Owner;
    public Vector3 IntendedTarget;
    public LayerMask collisionMask;

    public float DistanceTraveled { get { return this.distanceTraveled; } }
    

    Vector3 startingPosition;
    Vector3 lastPosition;
    float distanceTraveled = 0.0f;

    bool usePostImpactPenetration = false;

    private float startTime;
    GameEventSystem gameEventSystem;

    Quaternion startingRotation; // used to undo collision
    
    // TODO:  Destroy shell
    // TODO:  Explosion/Impact effect

    Vector3 lastContactPoint;
    Vector3 lastContactNormal;
    bool hasCollided;

    Collider[] OwnerColliders;
    

    public void SetImpactPenetration(float newPenetration)
    {
        if(newPenetration <= float.Epsilon)
        {
            RemoveShell();
        }
        else
        {
            usePostImpactPenetration = true;
            Penetration = newPenetration;

            

            // reset distance
            this.distanceTraveled = 0.0f;
            this.MaxRange = Penetration / 50.0f; // magic equation

            Debug.Log("Post Impact Pen:" + Penetration);
            Debug.Log("Post Impact Range:" + MaxRange);
        }
    }

    public float GetCurrentPenetration()
    {
        if (usePostImpactPenetration)
        {
            return Penetration;
        }
        else
        {
            return PenetrationDropoff.Evaluate(distanceTraveled / MaxRange) * Penetration;
        }
    }

	// Use this for initialization
	void Start () {
        var rigidBody = this.GetComponent<Rigidbody>();

        rigidBody.AddRelativeForce(0, 0, InitialVelocity * IsolatedTimeScale, ForceMode.VelocityChange);
    //    rigidBody.AddForce(Physics.gravity * IsolatedTimeScale, ForceMode.VelocityChange);
        
        rigidBody.solverIterations = 300;

        startTime = Time.time;
        startingPosition = lastPosition = this.transform.position;
        startingRotation = this.transform.rotation;

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        OwnerColliders = this.Owner.GetComponentsInChildren<Collider>(true);
	}
	
    void FixedUpdate()
    {
        //this.distanceTraveled += Vector3.Distance(transform.position, lastPosition);
        //lastPosition = this.transform.position;

        this.distanceTraveled = Vector3.Distance(startingPosition, this.transform.position);
    }

	// Update is called once per frame
	void Update ()
    {



        // Tempted's move ourself.  I tried rigidbodies, but was disappointed by a few things:
        // 1.  Physics engine inaccuracy at high speeds
        // 2.  Timescale doesn't work with high speed rigidbodies very well.
        // 3.  multiple collisions with the same object, even using ignore collision

        //if (hasCollided)
        //{
        //   // Debug.DrawRay(lastContactPoint, lastContactNormal, Color.red);

        //    //Time.timeScale = 0.0f;
        //}

        
	}

    void LateUpdate()
    {
        if ((Time.time - startTime) > LifeSpan)
        {
            Debug.Log("Out of time!");

            //   Time.timeScale = 0.00f;
            RemoveShell();
        }

        if (this.distanceTraveled >= MaxRange)
        {
            Debug.Log("Out of Range!:" + this.distanceTraveled);
            RemoveShell();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.collider.gameObject.GetComponent<DamageableComponent>() != null)
        {
            hasCollided = true;
            lastContactNormal = other.contacts[0].normal;
            lastContactPoint = other.contacts[0].point;

      //      Time.timeScale = 0.005f;
        }
        Debug.Log("Shell hit!" + other.collider.gameObject.name);
    }

    public void RemoveShell()
    {
        this.gameObject.layer = LayerMask.NameToLayer("NullLayer");
        gameEventSystem.RaiseEvent(GameEventType.BulletLifetimeOver, this.gameObject, this);
        DestroyObject(this.gameObject);
    }

    public void ResetAfterCollision(Collider otherCollider, Vector3 contactPoint)
    {
        // TODO:  Do i want to use a lower velocity here?
        var rigidBody = this.GetComponent<Rigidbody>();
        Physics.IgnoreCollision(GetComponent<Collider>(), otherCollider);

        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        this.transform.rotation = startingRotation;
        startingPosition = lastPosition = this.transform.position; //= contactPoint;

        ////// TODO:  Adjust forward?
        this.GetComponent<Rigidbody>().AddRelativeForce(0, 0, (InitialVelocity * IsolatedTimeScale)  , ForceMode.VelocityChange);
        Physics.IgnoreCollision(GetComponent<Collider>(), otherCollider);
        //var trail = this.GetComponent<TrailRenderer>();
        //trail.enabled = false;
       

        //GameObject newProjectile =
        // Instantiate(Shrapnel, transform.position, transform.rotation) as GameObject;

        //var shrapnelRigidBody = newProjectile.GetComponent<Rigidbody>();
        //Physics.IgnoreCollision(newProjectile.GetComponent<Collider>(), otherCollider);

        //shrapnelRigidBody.AddRelativeForce(0, 0, InitialVelocity * IsolatedTimeScale, ForceMode.VelocityChange);
    }

    public void SetupPenetration(float newPen, Collider otherCollider, Vector3 contactPoint)
    {
        LifeSpan = float.MaxValue;
        //Physics.IgnoreCollision(other.contacts[0].thisCollider, other.contacts[0].otherCollider);
        SetImpactPenetration(newPen);
        ResetAfterCollision(otherCollider, contactPoint);
    }
}
