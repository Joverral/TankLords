using UnityEngine;
using System.Collections;

public class NavMeshMarker : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        this.gameObject.SetActive(false);
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
