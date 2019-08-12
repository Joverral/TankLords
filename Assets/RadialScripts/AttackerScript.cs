using UnityEngine;
using System.Collections;

public class AttackerScript : MonoBehaviour {

     [SerializeField]
    public WeaponScript CurrentWeapon;

     public bool CanFire { get; set; }

    
    public Transform CurrentFacing()
     {
         return this.CurrentWeapon.MuzzlePosition;
     }

    public Transform FireWeapon()
    {
        CanFire = false;

        var projectile = this.CurrentWeapon.Fire();
        var shellScript = projectile.GetComponent<ShellScript>();
        shellScript.Owner = this.gameObject;

        return projectile;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
