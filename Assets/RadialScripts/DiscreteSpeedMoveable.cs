using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Speed
{
    Full = 3,
    Combat = 2,
    Neutral = 1,
    Reverse = 0
}



public class DiscreteSpeedMoveable : MonoBehaviour {

    public Speed currentSpeed = Speed.Combat;
    public Speed startingSpeed = Speed.Combat; //Speed at the start of the round/turn
    public float[] TurnRadiusArray = { 4.0f, 1.0f, 4.0f, 8.0f };
    public float[] DistanceArray = {30.0f, 10.0f, 60.0f, 100.0f };

    // Cost per meter
    public float[] SpeedCostArray = { 3.0f, 10.0f, 1.6f, 1.0f };

    public TileMoveable tileMoveable;

    public float CurrentTurnRadius { get { return TurnRadiusArray[(int)currentSpeed]; } }
    public float CurrentDistance { get { return DistanceArray[(int)currentSpeed]; } }

    GameEventSystem gameEventSystem;
	// Use this for initialization
	void Start () {
        this.startingSpeed = Speed.Combat;

        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();
     //   gameEventSystem.Subscribe(this.gameObject, GameEventType.PathChanged, PathChanged);


        SetSpeed(this.startingSpeed);
	}

    //public void PathChanged(GameObject go)
    //{
    //    if (go == this.gameObject)
    //    {
    //        this.currentSpeed = go.GetComponent<TileMoveable>().CurrentPath.speed;
    //        gameEventSystem.RaiseEvent(GameEventType.SpeedChanged, this.gameObject);
    //    }
    //}

	// Update is called once per frame
	void Update () {
	
	}

    public bool TryChangeSpeed(Speed newSpeed)
    {
        return true;
    }

    public void SetSpeed(Speed newSpeed)
    {
        // this.tileMoveable.turnRadius = this.TurnRadiusArray[(int)newSpeed];
        // this.tileMoveable.MaxMoveDistance = MovementPoints; // this.DistanceArray[(int)newSpeed];
        //this.tileMoveable.CurrentMoveDistance = tileMoveable.MaxMoveDistance;
        
        
        currentSpeed = newSpeed;

        this.tileMoveable.CurrentMoveDistance = CurrentDistance;
        this.tileMoveable.MaxMoveDistance = CurrentDistance;
        gameEventSystem.RaiseEvent(GameEventType.SpeedChanged, this.gameObject, newSpeed);
    }

    // It's a float because it's a broadcast from the slider, which is always flaot
    public void DiscreteSpeedChanged(float newSpeedFloat)
    {
        Speed newSpeed = (Speed)newSpeedFloat;

        if (newSpeed != this.currentSpeed &&
            tileMoveable.CurrentMoveDistance == tileMoveable.MaxMoveDistance)
        {
            // todo, check if the moveable has already moved
            SetSpeed(newSpeed);
        }
    }
}
