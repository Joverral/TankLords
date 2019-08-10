using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BehaviourStack : MonoBehaviour {

    public interface StackableMonoBehaviour
    {
        void OnEnter();
        void OnLeave();

    }
    Stack<MonoBehaviour> behaviourStack;

    public void Push(MonoBehaviour newMonoBehaviour)
    {
        LeaveCurrentBehaviour();

        var stackable = newMonoBehaviour as StackableMonoBehaviour;
        if (stackable != null)
        {
            stackable.OnEnter();
        }

        newMonoBehaviour.enabled = true;
    }

    public MonoBehaviour Pop()
    {
        LeaveCurrentBehaviour();

        if (behaviourStack.Count > 0)
        {
            var stackable = behaviourStack.Peek() as StackableMonoBehaviour;
            if (stackable != null)
            {
                stackable.OnEnter();
            }
            behaviourStack.Peek().enabled = true;
        }

        return behaviourStack.Pop();
    }

    private void LeaveCurrentBehaviour()
    {
        if (behaviourStack.Count > 0)
        {
            var stackable = behaviourStack.Peek() as StackableMonoBehaviour;
            if (stackable != null)
            {
                stackable.OnLeave();
            }

            behaviourStack.Peek().enabled = false;
        }
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
