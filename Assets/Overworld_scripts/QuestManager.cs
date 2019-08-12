using UnityEngine;
using System.Collections;

public class QuestManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public class Faction
{
    string Name;
    Vector2 Location;
    float strength;

    // array of all factions and their like/dislike ?  Maybe in a faction manager?
}


// Quest States:  Available, Active, Succeeded, Failed, Abandoned?
//  OnEnterState, OnExitState?