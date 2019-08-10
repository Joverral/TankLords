using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public interface PlayerState
{
    void OnConfirm();
    void OnCancel();
    void OnScreenPointerUp(PointerEventData pointerData);
    void OnScreenClick(PointerEventData pointerData);
    void OnScreenPointerDown(PointerEventData pointerData);
    void OnScreenDrag(PointerEventData pointerData);
    void OnMove(PointerEventData axisData);
    void OnObjectSelected(GameObject go);
    void OnEnter();
    void OnLeave();

    void UpdateButtons(); //Change to just Update?
}

public class PlayerController : MonoBehaviour {
    [SerializeField]
    PlayerMovementState movementState;
    [SerializeField]
    PlayerTurretState playerTurretState;
    [SerializeField]
    PlayerAttackState playerAttackState;
    
    Stack<PlayerState> playerStateStack = new Stack<PlayerState>(10);

    public LayerMask unitLayer;
    
    // Generic HUD objects
    public SelectionPanelScript selectionPanel;
    public GameObject selectedGameObject;

    GameEventSystem gameEventSystem;
    public Button confirmButton;
    public Button cancelButton;


    // Quick hack for better profiling

    List<Action> actionList = new List<Action>();

    void Start()
    {
        PushState(movementState); // go with movementstate for default?  for now...

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        gameEventSystem.Subscribe(this.gameObject, GameEventType.PathChanged, OnPathChange);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.TurnEnded, OnTurnEnd);
    }


    // TODO: I'm torn on this one
    public void SelectObject(GameObject go)
    {
        selectedGameObject = go;
        selectionPanel.SelectObject(go);
    }

	public void ScreenClick(BaseEventData data)
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            var pointerData = data as PointerEventData;
            topState.OnScreenClick(pointerData);
        }
    }

    public void ScreenPointerUp(BaseEventData data)
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            var pointerData = data as PointerEventData;
            topState.OnScreenPointerUp(pointerData);
        }
    }

    public void ScreenBegingDrag(BaseEventData data)
    {
     
    }
    public void ScreenEndDrag(BaseEventData data)
    {
     
    }
    public void ScreenPointerDown(BaseEventData data)
    {
       actionList.Add(() =>
       {
           if (playerStateStack.Count > 0)
           {
               var topState = playerStateStack.Peek();
               var pointerData = data as PointerEventData;
               topState.OnScreenPointerDown(pointerData);
           }
       });
     
        
    }
    public void ScreenInitializeDrag(BaseEventData data)
    {
    }

    public void ScreenDrag(BaseEventData data)
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            var pointerData = data as PointerEventData;
            topState.OnScreenDrag(pointerData);
        }
    }

    public void ScreenPointerEnter(BaseEventData data)
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            var pointerData = data as PointerEventData;


            topState.OnMove(pointerData);

            //Debug.Log("Halllujah X");   
        }
    }

    // When the UI changes the DiscreteSpeed, this function is called
    public void DiscreteSpeedChanged(float value)
    {
        if (selectedGameObject != null)
        {
            this.selectedGameObject.BroadcastMessage("DiscreteSpeedChanged", value, SendMessageOptions.RequireReceiver);
            //this.controlWidget.BroadcastMessage("DiscreteSpeedChanged", SendMessageOptions.RequireReceiver);
        }
    }

    public void ConfirmButton_Click()
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            topState.OnConfirm();
        }
    }

    public void CancelButton_Click()
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            topState.OnCancel();
        }
    }

    private void UpdateButtons()
    {
        if (playerStateStack.Count > 0)
        {
            var topState = playerStateStack.Peek();
            topState.UpdateButtons();
        }
    }

    public void OnTurnEnd(GameObject sender, System.Object data)
    {
        if (selectedGameObject != null)
        {
            UpdateButtons();
        }
    }

    public void OnPathChange(GameObject sender, System.Object data)
    {
        if (selectedGameObject == sender)
        {
            UpdateButtons();
        }
    }

    public void RotateTurretButton_Click()
    {
        // TODO:  what should I do when they click during a move?
        if (selectedGameObject != null)
        {
            var tileMoveable = selectedGameObject.GetComponent<TileMoveable>();
            if (tileMoveable == null || !tileMoveable.isMoving)
            {
                PushState(playerTurretState);
            }
        }
    }

    public void AttackButton_Click()
    {
        // TODO:  what should I do when they click attack during a move?
        if(selectedGameObject != null)
        {
            var tileMoveable = selectedGameObject.GetComponent<TileMoveable>();
            if(tileMoveable == null || !tileMoveable.isMoving)
            {
                //TODO;  I should disable/gray out the attack button instead...
                //if (!playerStateStack.Contains(playerAttackState)) //bullet cam state...
                //if( !(playerStateStack.Peek().GetType() as  BulletCamState))
                {
                    PushState(playerAttackState);
                } 
            }
        }
    }

    public void MoveButton_Click()
    {
        if (selectedGameObject != null)
        {
            PushState(this.movementState);
        }
    }

    void Update()
    {
        for(int i = 0; i < actionList.Count; i++)
        {
            actionList[i].Invoke();
        }

        actionList.Clear();
    }

    // Internal push state does not try to leave/deactivate the previous state
    private void InternalPush(PlayerState playerState)
    {
        ActivateState(playerState);
        playerStateStack.Push(playerState);
        playerState.OnEnter();
    }

    // Internal pop does try to re-activate next state (or add the default if not there)
    private void InternalPop()
    {
        PlayerState exitingState = playerStateStack.Pop();
        LeaveState(exitingState);
    }

    private void ActivateState(PlayerState playerState)
    {
        Debug.Log("Activating State: " + playerState.GetType());
        MonoBehaviour enteringMonoBehaviour = playerState as MonoBehaviour;
        if (enteringMonoBehaviour != null)
        {
            enteringMonoBehaviour.enabled = true;
        }
    }

    //Technically this replaces the current state already...
    public void PushState(PlayerState playerState)
    {
        if(playerStateStack.Count > 0)
        {
            PlayerState oldState = playerStateStack.Peek();
            Debug.Log("PushState:  Deactivate Old State: " + oldState.GetType());
            LeaveState(oldState);
        }

        InternalPush(playerState);
    }

    // Replaces the current state with the new state
    public void ReplaceState(PlayerState playerState)
    {
        InternalPop();
        InternalPush(playerState);
    }

    void LeaveState(PlayerState exitingState)
    {
        Debug.Log("LeaveState:" + exitingState.GetType());
        exitingState.OnLeave();

        MonoBehaviour exitingMonoBehaviour = exitingState as MonoBehaviour;
        if (exitingMonoBehaviour != null)
        {
            
            exitingMonoBehaviour.enabled = false;
        }
    }

    public void PopCurrentState()
    {
        if (playerStateStack.Count > 0)
        {
            PlayerState exitingState = playerStateStack.Pop();
            Debug.Log("PopCurrentState:  ExitingState: " + exitingState.GetType());
            LeaveState(exitingState);
          
            if (this.playerStateStack.Count == 0)
            {
                Debug.Log("Pushing Default State!");
                // push the default state
                InternalPush(movementState);
            }
            else
            {
                ActivateState(playerStateStack.Peek());
                playerStateStack.Peek().OnEnter();
            }
        }
    }
}
