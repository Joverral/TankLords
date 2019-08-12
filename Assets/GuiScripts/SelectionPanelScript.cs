using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SelectionPanelScript : MonoBehaviour {

    //TODO I should probably break this out into it's component, instead of creating this uber selectionpanel
    // TODO:  Possible intermediary step:  Move it out into per panel?
    public Text displayText;
    public Slider movementSlider;
    public Slider movementPotentialUsageSlider;
    public Text movementSliderText;
    public Toggle speedLock;
    public Text speedText;

    // TODO:  Move these to separate scripts
    public Slider discreteSpeedSlider;
    private DiscreteSpeedMoveable discreteSpeedMoveable;

    public Slider turretDegreeSlider;
    public Slider turretDegreeUsageSlider;

    TileMoveable tileMoveable;
    TurretScript turret;

    GameEventSystem gameEventSystem;

    // Use this for initialization
    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        gameEventSystem.Subscribe(this.gameObject, GameEventType.PathChanged, PathChanged);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.SpeedChanged, SpeedChanged);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.ObjectPropertyChanged, DistanceChanged);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.MoveStarted, MoveStarted);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.TurnEnded, TurnEnded);
    }

    void UpdatePotentialSlider()
    {
        if (tileMoveable != null)
        {
            movementPotentialUsageSlider.maxValue = tileMoveable.CurrentMoveDistance;
            
            RectTransform rectTrans = movementPotentialUsageSlider.GetComponent<RectTransform>();
            float moveSliderWidth  = movementSlider.GetComponent<RectTransform>().rect.width;

            rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (tileMoveable.CurrentMoveDistance / tileMoveable.MaxMoveDistance) * moveSliderWidth);
            rectTrans.localPosition =

            new Vector3(
              -((tileMoveable.MaxMoveDistance - tileMoveable.CurrentMoveDistance) / tileMoveable.MaxMoveDistance) * (moveSliderWidth / 2.0f),
              movementPotentialUsageSlider.GetComponent<RectTransform>().localPosition.y,
              movementPotentialUsageSlider.GetComponent<RectTransform>().localPosition.z);

            if (tileMoveable.CurrentPath == null || tileMoveable.CurrentPath.path.Count == 0)
            {
                movementPotentialUsageSlider.value = 0;
                movementPotentialUsageSlider.enabled = false;
            }
            else
            {
                movementPotentialUsageSlider.enabled = true;
                //TODO:  I should look at distanec traveled or something
                movementPotentialUsageSlider.value = tileMoveable.CurrentPath.CalculateTotalDistance() * tileMoveable.CurrentPath.moveRateCost;
            }
        }
    }

    void UpdateDiscreteSpeedSlider()
    {
        if (tileMoveable)
        {
            movementSlider.maxValue = tileMoveable.MaxMoveDistance;
            movementSlider.value = tileMoveable.CurrentMoveDistance;

            movementSliderText.text = string.Format("MP: {0:0.#}/{1}", tileMoveable.CurrentMoveDistance, tileMoveable.MaxMoveDistance);

            if ( discreteSpeedMoveable != null)
            {
                if (discreteSpeedSlider != null)
                {
                    //Debug.Log("DiscreteSpeedSlider Value:" + (int)discreteSpeedMoveable.currentSpeed);
                    discreteSpeedSlider.value = (int)discreteSpeedMoveable.currentSpeed;
                }

                if (this.speedText != null)
                {
                    speedText.text = "Speed: " + this.discreteSpeedMoveable.currentSpeed.ToString();
                }
            }


        }
    }

    public void SelectObject(GameObject go)
    {
        this.displayText.text = go.name;
        tileMoveable = go.GetComponent<TileMoveable>();
        discreteSpeedMoveable = go.GetComponent<DiscreteSpeedMoveable>();

        if(tileMoveable != null)
        {
            if(tileMoveable.MaxMoveDistance == tileMoveable.CurrentMoveDistance)
            {
                UnlockSpeedPanel();
            }
            else
            {
                LockSpeedPanel();
            }
        }
        UpdatePotentialSlider();
        UpdateDiscreteSpeedSlider();

        turret = go.GetComponent<TurretScript>();
        if(turret != null)
        {

        }
    }

    public void PathChanged(GameObject sender, System.Object data)
    {
        //Debug.Log("Path Changed!!");
        UpdatePotentialSlider();
    }

    public void SpeedChanged(GameObject sender, System.Object data)
    {
        Debug.Log("Speed Changed!!");
        UpdateDiscreteSpeedSlider();

        
    }

    public void DistanceChanged(GameObject sender, System.Object data)
    {
		if (sender == this.tileMoveable.gameObject)
        {
            movementSlider.maxValue = tileMoveable.MaxMoveDistance;
            movementSlider.value = tileMoveable.CurrentMoveDistance;

            movementSliderText.text = string.Format("MP: {0:0.#}/{1}", tileMoveable.CurrentMoveDistance, tileMoveable.MaxMoveDistance);
        }
    }

    public void LockSpeedPanel()
    {
        speedLock.isOn = true;
        discreteSpeedSlider.interactable = false;
    }

    public void UnlockSpeedPanel()
    {
        speedLock.isOn = false;
        discreteSpeedSlider.interactable = true;
    }

    public void MoveStarted(GameObject sender, System.Object data)
    {
        if (sender == this.tileMoveable.gameObject)
        {
            LockSpeedPanel();
        }
    }

    public void TurnEnded(GameObject sender, System.Object data)
    {
        UnlockSpeedPanel();
    }
}
