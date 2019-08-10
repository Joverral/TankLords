using UnityEngine;
using System.Collections;

public class WeaponScript : MonoBehaviour {

    public Transform WeaponMount;
    public string Name;
    public float arc;
    public Transform MuzzlePosition;

    //public weaponblueprint

    // these should probably come from the blueprint
    public GameObject Projectile;
    public GameObject MuzzleEffect;
 
    public AudioSource AudioSource;


    public Transform Fire()
    {
        // TODO:  Accuracy
        // TODO:  Muzzle Effect
        GameObject newProjectile = 
            Instantiate(Projectile, MuzzlePosition.position, MuzzlePosition.rotation) as GameObject;


       
        //this.audioSource.clip = this.audioClip;
        //this.audioSource.pitch = Random.Range(.8f, 1.0f);
        //this.audioSource.Play();

        return newProjectile.transform;
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
