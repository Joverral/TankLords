using UnityEngine;
using System.Collections;

public class SetTargetFromMouse : MonoBehaviour {

    UnityEngine.AI.NavMeshAgent navAgent;

	// Use this for initialization
	void Start () {
        navAgent = this.GetComponent<UnityEngine.AI.NavMeshAgent>();
	}
	
	// Update is called once per frame
	void Update () {
	

	}
}
