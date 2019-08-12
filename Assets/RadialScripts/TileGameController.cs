using UnityEngine;
using System.Collections;

public class TileGameController : MonoBehaviour {

    GameEventSystem gameEventSystem;

	// Use this for initialization
	void Start () {
        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();
        
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnEndTurn_Clicked()
    {
        this.BroadcastMessage("OnEndTurn");
        gameEventSystem.RaiseEvent(GameEventType.TurnEnded, this.gameObject, this.gameObject);
    }
}
