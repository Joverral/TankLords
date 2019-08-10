using UnityEngine;
using System.Collections;

public class NavigateToTarget : MonoBehaviour {

    UnityEngine.AI.NavMeshAgent navMeshAgent;

    [SerializeField]
    Transform navMarker;
    [SerializeField]
    float markerHeight = 1.0f; // Don't like this here.

	void Start () {
        navMeshAgent = this.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if(navMarker == null)
        {
            Debug.LogError("NavigateToTarget Script is missing navMarker", this);
        }
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetMouseButtonUp(1))
        {
            var screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hitInfo;
            if (Physics.Raycast(screenRay, out hitInfo))
            {
                navMeshAgent.destination = hitInfo.point;
                SetNavMarker();
            }
        }
	}

    private void SetNavMarker()
    {
        navMarker.position = navMeshAgent.destination + new Vector3(0.0f, markerHeight, 0.0f);
        navMarker.gameObject.SetActive(true);
    }
}
