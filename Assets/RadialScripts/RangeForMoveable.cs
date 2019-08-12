using UnityEngine;
using System.Collections;

public class RangeForMoveable : MonoBehaviour
{
    TileMoveable selectedMoveable;

	// Update is called once per frame
	void Update () {
        // TODO should toss a ifnot different on here.
        if (selectedMoveable != null) //&& selectedMoveable.isMoving
        {
            UpdateToMoveable();
        }
	}

    void UpdateToMoveable()
    {
        this.transform.position = selectedMoveable.transform.position;
        this.transform.localScale = new Vector3(selectedMoveable.CurrentMoveDistance,
                                                    selectedMoveable.CurrentMoveDistance,
                                                    selectedMoveable.CurrentMoveDistance);
    }

    public void UseForMoveable(TileMoveable moveable)
    {
        selectedMoveable = moveable;
        UpdateToMoveable();
    }
}
