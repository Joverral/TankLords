using UnityEngine;
using System.Collections;

public class GuiRotationScript : MonoBehaviour {

    public Transform attachTransform;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (attachTransform != null)
        {
            this.transform.position = attachTransform.position + Vector3.up * 1.0f;
            this.transform.forward = attachTransform.transform.forward;
            this.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);
        }
	}
}
