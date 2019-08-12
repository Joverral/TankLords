using UnityEngine;
using System.Collections;

public class TurningRadiusForMoveable : MonoBehaviour {

    TileMoveable selectedMoveable;
    public LineRenderer leftCircleRenderer;
    public LineRenderer rightCircleRenderer;
    public GameObject leftArrow;
    public GameObject rightArrow;

    void Start()
    {
        leftArrow.transform.localPosition = Vector3.left;
        rightArrow.transform.localPosition = Vector3.right;

        leftArrow.transform.Rotate(Vector3.up, Mathf.PI * Mathf.Rad2Deg, Space.Self);
        rightArrow.transform.Rotate(Vector3.up, Mathf.PI * Mathf.Rad2Deg, Space.Self);
    }

	// Update is called once per frame
	void Update () {
        // TODO should toss a ifnot different on here.
        if (selectedMoveable != null && !selectedMoveable.isMoving)
        {
            UpdateToMoveable();
        }
	}


    void UpdateToMoveable()
    {
        this.transform.position = selectedMoveable.transform.position + Vector3.up;

        if (selectedMoveable.CurrentPath != null)
        {
            this.transform.forward = selectedMoveable.CurrentPath.isReverse ? 
                                        -selectedMoveable.transform.forward : selectedMoveable.transform.forward;

            float turnRadius = selectedMoveable.CurrentPath.turnRadius;

            Vector3 scale = new Vector3(turnRadius,
                                        turnRadius,
                                       turnRadius);


            this.transform.localScale = scale;
            Vector3 invScale = new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z);

            // TODO: Get rid of magic number 0.25f
            this.leftArrow.transform.localScale = selectedMoveable.ModelWidth * 0.25f * invScale;
            this.rightArrow.transform.localScale = selectedMoveable.ModelWidth * 0.25f * invScale;

        }
        else
        {
            this.transform.forward = selectedMoveable.transform.forward;
        }

        this.rightCircleRenderer.SetWidth(selectedMoveable.ModelWidth, selectedMoveable.ModelWidth);
        this.leftCircleRenderer.SetWidth(selectedMoveable.ModelWidth, selectedMoveable.ModelWidth);


        // TODO:  Turn off while moving, turn on after
     
    }

    public void UseForMoveable(TileMoveable moveable)
    {
        selectedMoveable = moveable;
        UpdateToMoveable();
    }
}
