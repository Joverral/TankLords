using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class BaseGameEventData
{
    public BaseGameEventData(GameObject go)
    {
        Sender = go;
    }

    public readonly GameObject Sender;
}

public class ObjectPropertyChangedEvent : BaseGameEventData
{
    // TODO add proprety changed fields or something
    public ObjectPropertyChangedEvent(GameObject go) : base(go)
    {

    }
}

public enum GameEventType
{
    SelectedObjectChanged,
    ObjectPropertyChanged,
    PathChanged,
    SpeedChanged,
    MoveStarted,
    MoveEnded,
    TurnEnded,
    MovementModeEntered,
    MovementModeLeft,
    AttackModeEntered,
    AttackModeLeft,
    TargetOrientationChanged,
    BulletLifetimeOver,
    SuccessfulArmorPenetration,
    TurretOrientationChanged
}

public class GameEventSystem : MonoBehaviour {


    struct ReferenceEventPair
    {
        // I'm cheesing this... to avoid dynamic invoke
        public readonly WeakReference WeakRef;
        public readonly Action<GameObject, System.Object> Action;

        public ReferenceEventPair(WeakReference weakRef, Action<GameObject, System.Object> action)
        {
            WeakRef = weakRef;
            Action = action;
        }
    }

    Dictionary<GameEventType, List<ReferenceEventPair>> eventTable = new Dictionary<GameEventType,List<ReferenceEventPair>>();

	
	// Update is called once per frame
	void Update () {
	
	}

    // This is terrible TBD
    public void Subscribe(GameObject subscriber, GameEventType gameEvent, Action<GameObject, System.Object> eventResponse)
    {
        ReferenceEventPair newPair = new ReferenceEventPair(new WeakReference(subscriber), eventResponse);

        if (!eventTable.ContainsKey(gameEvent))
        {
            eventTable.Add(gameEvent, new List<ReferenceEventPair>());
        }

        // TODO: Check for existing subscription
        eventTable[gameEvent].Add(newPair);
    }

    public void RaiseEvent(GameEventType gameEvent, GameObject sender, System.Object data)
    {
        if (eventTable.ContainsKey(gameEvent))
        {
            List<ReferenceEventPair> references = eventTable[gameEvent];

            for (int i = 0; i < eventTable[gameEvent].Count; ++i)
            {
                //GameObject targetGo = references[i].WeakRef.Target as GameObject;
                if (references[i].WeakRef.IsAlive)
                {
                    references[i].Action.Invoke(sender, data); // Still not sure how much better this is...
                    // TODO:  Yeah, this is kind of kludgey
                    // A system where do something via a concrete event or even a hash would be better
                    // targetGo.SendMessage(gameEvent.ToString(), gameEventData, SendMessageOptions.RequireReceiver);

                }
                // TODO:  I should probably remove the object if it's not alive...
            }
        }
    }

    //public void RaiseEvent(GameEventType gameEvent, GameObject go)
    //{
    //    if (eventTable.ContainsKey(gameEvent))
    //    {
    //        List<ReferenceEventPair> references = eventTable[gameEvent];

    //        for (int i = 0; i < eventTable[gameEvent].Count; ++i)
    //        {
    //            GameObject targetGo = references[i].WeakRef.Target as GameObject;
    //            if (references[i].WeakRef.IsAlive)
    //            {
    //                references[i].Action.Invoke(go);
    //                // TODO:  Yeah, this is kind of kludgey
    //                // A system where do something via a concrete event or even a hash would be better
    //                // targetGo.SendMessage(gameEvent.ToString(), gameEventData, SendMessageOptions.RequireReceiver);

    //            }
    //        }
    //    }
    //}
}
