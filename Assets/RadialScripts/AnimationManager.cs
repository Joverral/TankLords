using UnityEngine;
using System.Collections;

public class AnimationManager : MonoBehaviour {

    public Animation LeftTrackAnimation;

    GameEventSystem gameEventSystem;

	// Use this for initialization
	void Start () {
        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
