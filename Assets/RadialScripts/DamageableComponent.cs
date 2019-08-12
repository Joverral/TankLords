using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO:  Not sure the best way to do the damage system yet.
// maybe I should inherit from DamgeableComponent each of these types?
public enum DamageType
{
    Armor,
    Engine,
    Transmission,
    LeftTrack,
    RightTrack,
    TurretTraverse,
    Weapon,
    Crew_Gunner,
    Crew_Loader,
    Crew_Commander,
    Crew_Driver,
    Crew_RadioOp,
    Crew_Loader_2,
    Crew_Gunner_2,
}

//public enum DamageState
//{ //how much of a modifer to how well it works.
//    Normal =    1.0f,
//    Light =     0.75f,
//    Medium =    0.5f,
//    Heavy =     0.25f,
//    Destroyed = 0.0f
//}

[RequireComponent(typeof(Collider))]
public class DamageableComponent : MonoBehaviour //, ISerializationCallbackReceiver
{
    const float kRicochetAngleInRadians = Mathf.Deg2Rad * 70.0f;
    const float kMinPen = 0.75f;
    const float kMaxPen = 1.25f;

    [SerializeField]
    DamageType DamageType = DamageType.Armor;

    [SerializeField]
    float CurrentDamageModifier = 1.0f;

    [SerializeField]
    float Armor = 35.0f; //todo? Front, Side, Rear?
    [SerializeField]
    Texture2D collisionTexture;

    [SerializeField]
    GameObject HighlightModel; // when hit this is briefly enabled, and it's shader is tweaked

    private MeshRenderer[] hightlightMeshes;
    float hightlightCountdownTimer = 0.0f;

    [SerializeField]
    AnimationCurve GlowCurve;

    [SerializeField]
    float HighlightAnimationTimeInSeconds = 1.0f;


    public GameObject InternalModel; // This is turned on when a penetration occurs (via model controller

    Vector3 lastContactPoint;
    Vector3 lastContactNormal;
    bool hasCollided;

    GameEventSystem gameEventSystem;
    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        if (HighlightModel != null)
        {
            hightlightMeshes = HighlightModel.GetComponentsInChildren<MeshRenderer>();
        }

    }

    private void SetHighlightGlow(float value)
    {
        for (int i = 0; i < hightlightMeshes.Length; ++i)
        {
            hightlightMeshes[i].material.SetFloat("_MKGlowPower", value);
            hightlightMeshes[i].material.SetFloat("_MKGlowTextureStrength", value);
        }
    }

    public void StartHighlight()
    {
        // todo: turn off internal representation (if any)

        // set the glow start
        SetHighlightGlow(GlowCurve.Evaluate(0.0f));

        for(int i = 0; i < hightlightMeshes.Length; ++i)
        {
            hightlightMeshes[i].enabled = true;
        }

       

        hightlightCountdownTimer = HighlightAnimationTimeInSeconds;
    }

    public void StopHighlight()
    {
        for (int i = 0; i < hightlightMeshes.Length; ++i)
        {
            hightlightMeshes[i].enabled = false;
        }
    }

    void Update()
    {
        if (hasCollided && hightlightCountdownTimer > 0.0)
        {
            hightlightCountdownTimer -= Time.unscaledDeltaTime;

            
            //Debug.Log("tValue Value: " + tValue.ToString(), this);
            
            //Debug.Log("Glow Value: " + val.ToString(), this);

            
            float tValue = 1.0f - (hightlightCountdownTimer / HighlightAnimationTimeInSeconds);
            float val = GlowCurve.Evaluate(1.0f - (tValue));

            SetHighlightGlow(val);
            if(hightlightCountdownTimer <= 0.0f)
            {
                StopHighlight();
            }
        }

        if(hasCollided)
        {
            //Debug.DrawRay(lastContactPoint, lastContactNormal, Color.yellow);
        }
    }
    float GetArmorValue(ContactPoint contact)
    {

        var meshCollider = this.GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            RaycastHit hit;
            const float rayLength = 1.0f;
            Ray ray = new Ray(contact.point - contact.normal * rayLength * 0.5f, contact.normal);

            Debug.DrawRay(ray.origin, ray.direction, Color.yellow);
            //Color C = Color.white; // default color when the raycast fails for some reason ;)
            if (meshCollider.Raycast(ray, out hit, rayLength))
            {
                Vector2 collisionPixelUV = hit.textureCoord;
                collisionPixelUV.x *= collisionTexture.width;
                collisionPixelUV.y *= collisionTexture.height;

                Color hitColor = collisionTexture.GetPixel((int)collisionPixelUV.x, (int)collisionPixelUV.y);

                Debug.Log("HitColor: " + hitColor);
                Debug.Log("Hit Texture Armor: " + hitColor.b * 255.0f);

                return hitColor.b * 255.0f;
            }

            Debug.LogError("Unexpectedly missed Raycast on mesh");
            return -1.0f;
        }

        return Armor;
    }
    void OnCollisionEnter(Collision other)
    {
        var shell = other.gameObject.GetComponent<ShellScript>();

        hasCollided = true;
        lastContactNormal = other.contacts[0].normal;
        lastContactPoint = other.contacts[0].point;

        if (shell != null)
        {
            Debug.Log("I was hit by a shell! " + this.gameObject.name);

            // Get the current penetration
            float pen = shell.GetCurrentPenetration();

            float cosRadians = Vector3.Dot(other.contacts[0].normal, shell.transform.forward);

            // Note that WarT has a random ricochet chance depending on angle / shell type
            //  WoT has a always ricochet

            // TODO:  Add a richochet cancel is pen is 3x the armor
            if( Mathf.Abs(cosRadians) > kRicochetAngleInRadians)
            {
                Debug.Log("Richochet!"); // I should do something here...maybe adjust the forward to fake a ricochet?
                // I may not have to do jack, just let the physics handle it.
                shell.RemoveShell();
            }
            else
            {
                // adjust armor to the direction of impact
                float adjustedArmor = this.GetArmorValue(other.contacts[0]) / cosRadians;
                Debug.Log("Armor: " + Armor); 
                Debug.Log("Adjusted Armor: " + adjustedArmor);

                float newPen = pen * Random.Range(kMinPen, kMaxPen);
                Debug.Log("Adjusted Pen By Random Amount:" + newPen);

                newPen -= adjustedArmor;

                if (newPen > Mathf.Epsilon)
                {
                    Debug.Log("Penetrated!"); 
                    
                    //TODO: this could probably all go in the shell
                    shell.SetupPenetration(newPen, other.contacts[0].thisCollider, other.contacts[0].point);

                    // TODO:  I think i'm better off spawning a shell 'fragment' here instead.

                    gameEventSystem.RaiseEvent(GameEventType. SuccessfulArmorPenetration, this.gameObject, this);

                    // how to determine damage? Example: A track is hit, I guess i just raise an event to the TileMoveable
                    // that it got hti wtih a shell of type X, and it 

                    StartHighlight();
                }
                else
                {
                    Debug.Log("Failed to penetrate"); 
                    shell.RemoveShell();
                }
            }
        }
    }
}
