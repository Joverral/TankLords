using UnityEngine;
using System.Collections;

public class ActionProvider : MonoBehaviour {

    // list of actions
    // or should i be more specific, QuestProvider, which provide Quests?
    // (get) Quests, Shop, Repair, Hire, ?Investigate?, Attack / Raid / Ambush, ?Defend?, Flee

    // Additive (if not additive, and higher priority, then it will push itself onto the GUI stack
    // Priority (high, medium, low)
    // Blocking -- Pause game, disallow movement?


    void OnTriggerEnter(Collider other)
    {
     
        // push menu onto stack
        // 
    }
}


// Quests
// Convoy, Defend (location, target(s)), Ambush, Assasinate, Destroy targets, Recon, Patrol, Evac, Test Prototype, 
// Opposition:   Hidden, Swapped, Known, mostly Known, Trap 
// 