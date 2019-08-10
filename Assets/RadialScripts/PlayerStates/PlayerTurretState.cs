using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerTurretState : MonoBehaviour, PlayerState
{
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    TurretWidgetScript turretWidget;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    public void OnConfirm()
    {
        //throw new System.NotImplementedException();
    }

    public void OnCancel()
    {
        //throw new System.NotImplementedException();
    }

    public void OnScreenPointerUp(PointerEventData pointerData)
    {
        //  throw new System.NotImplementedException();
    }

    public void OnScreenClick(PointerEventData pointerData)
    {
        bool handledClick = turretWidget.OnPointerClick(pointerData);

        if (!handledClick)
        {
            // if the widget didn't handle the click, lets default to object selection
            // users can click on a new unit.
        }
        // RaycastHit hitInfo;
        //Ray ray = Camera.main.ScreenPointToRay(pointerData.position);

        //if (Physics.Raycast(ray, out hitInfo, 1000.0f, playerController.unitLayer))
        //{
    }

    public void OnScreenPointerDown(PointerEventData pointerData)
    {
        //  throw new System.NotImplementedException();
    }

    public void OnScreenDrag(PointerEventData pointerData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnObjectSelected(GameObject go)
    {
        //throw new System.NotImplementedException();
    }

    public void OnMove(PointerEventData axisData)
    {

    }

    public void OnEnter()
    {
        if (playerController.selectedGameObject != null)
        {
            var turret = playerController.selectedGameObject.GetComponent<TurretScript>();
            if (turret != null)
            {
                turretWidget.SetSelectedTurret(turret);
            }
        }
    }

    public void OnLeave()
    {
        turretWidget.Hide();

    }

    public void UpdateButtons()
    {
        //throw new System.NotImplementedException();
    }
}
