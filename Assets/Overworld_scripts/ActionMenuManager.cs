using UnityEngine;
using System.Collections;
using System;

public class ActionMenuManager : MonoBehaviour {

    [SerializeField]
    GameObject HorzLayout;

    public void ClearActions()
    {
        // remove children from horzlayout
    }

    public void AddActions(OverworldAction[] overworldActions)
    {

    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public  class
OverworldAction
{
    string displayText;
    Action someAction; // ?
}