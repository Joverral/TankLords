using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface ITransitionCanvas
{
    void TransitionIn();
    void TransitionOut();

    float Priority { get; }
    bool ShouldPause { get; }
}

public class MenuManager : MonoBehaviour {

    // The menu manager assumes that the bottom panel is the HUD, and should never go away
    Stack<ITransitionCanvas> panelStack = new Stack<ITransitionCanvas>();
    const int kMinPanelCount = 1;

    // Use this for initialization
    void Start () {
        //panelStack.Push(Hud);
    }

    // Update is called once per frame
    void Update () {
	
	}

    private void PushPanel(ITransitionCanvas newPanel)
    {
        PauseGame();

        var baseNewPanel = (MonoBehaviour)newPanel;
        // slight kludge, since we never make them inactive
        baseNewPanel.gameObject.SetActive(true);

        if (panelStack.Count > 0)
        {
            panelStack.Peek().TransitionOut();
        }

        newPanel.TransitionIn();
        panelStack.Push(newPanel);
    }

    public void Pop()
    {
        panelStack.Pop().TransitionOut();

        if (panelStack.Count > 0)
        {
            panelStack.Peek().TransitionIn();

            if (panelStack.Count == kMinPanelCount)
            {
                UnpauseGame();
            }
        }
    }

    public void PopToHUD()
    {
        while (panelStack.Count != kMinPanelCount)
        {
            panelStack.Pop().TransitionOut();
        }

        panelStack.Peek().TransitionIn();

        UnpauseGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0.0f;
    }

    private void UnpauseGame()
    {
        Time.timeScale = 1.0f;
    }
    
}

