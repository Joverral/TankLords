using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class ControlWidgetScript : MonoBehaviour {
	
	public TileMoveable tileMoveable;
	
	public LayerMask widgetLayer;
	public LayerMask terrainLayer;
	
    public AccessibleLineRenderer canMoveLineRenderer;
    public AccessibleLineRenderer pastMoveLineRenderer;
    public Shader ghostShader;

    public float maxScale = 1.3f;
    public float minScale = 0.3f;
    public GameObject[] childParts;

    //TODO make this dynamically dependent on camera zoom level
    public float Granularity = 1.0f;

    MoveablePath finalPath;

	bool wasDownOnControl = false;
    bool pathStartedAsReverse = false;
    bool draggingHasOccured = false;
    
    const int kLeftMouse = -1;
    const int kRightMouse = -2;
    const int kMiddleMouse = -3;

    GameEventSystem gameEventSystem;

    BoxCollider ghostCollider;

	enum eWidgetMode
	{
		Translate,
		Rotate
	}
	eWidgetMode moveMode;

    GameObject ghostClone;

	// Use this for initialization
	void Start () {
	
        //if(tileMoveable != null)
        //{
        //    SetPositionAndDirection(tileMoveable.transform.position, tileMoveable.transform.forward);
        //}
        //else
       // {
            HideControls();
        //}


        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();

        gameEventSystem.Subscribe(this.gameObject, GameEventType.SpeedChanged, DiscreteSpeedChanged);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.MoveEnded, MoveEnded);
        gameEventSystem.Subscribe(this.gameObject, GameEventType.TurnEnded, TurnEnded);
	}
	
	// Update is called once per frame
	void Update ()
	{
        // TODO:  don't bother if the T value hasn't changed
        float t = Camera.main.GetComponent<CameraControlScript>().AltitudeAsTvalue();
        Vector3 minScaleVec = new Vector3(minScale, minScale, minScale);
        Vector3 maxScaleVec = new Vector3(maxScale, maxScale, maxScale);

        if (this.childParts == null)
            return;

        for(int i = 0; i < this.childParts.Length; ++i)
        {
            this.childParts[i].transform.localScale = Vector3.Lerp(minScaleVec, maxScaleVec, t);
        }
        
        if(tileMoveable && tileMoveable.isMoving)
        {
            // this could probably be optimized
			int flipIdx = int.MaxValue;
            for (int i = 0; i < this.canMoveLineRenderer.Length; ++i)
            {
                if ((canMoveLineRenderer.GetPosition(i) - tileMoveable.transform.position).sqrMagnitude < 5.0f)
                {
					flipIdx = i;
                    break;
                }
            }

            for (int i = flipIdx; i < canMoveLineRenderer.Length; ++i)
			{
                canMoveLineRenderer.SetPosition(i, tileMoveable.transform.position);
			}
        }
	}

    public void OnPointerUp(PointerEventData eventData)
    {
        wasDownOnControl = false;
     }

    public void OnPointerDown(PointerEventData eventData)
    {
        draggingHasOccured = false;
        if (this.tileMoveable != null)
        {
            if (eventData.pointerId == kLeftMouse)
            {
                RaycastHit hitInfo;
                Ray ray = Camera.main.ScreenPointToRay(eventData.pressPosition);
                if (ghostCollider.Raycast(ray, out hitInfo, 1000.0f))
                {
                    moveMode = eWidgetMode.Translate;
					wasDownOnControl = true;
                }
                // TODO: Clean this up
                else if (Physics.Raycast(ray, out hitInfo, 1000.0f, widgetLayer))
                {
                    if (string.Compare(hitInfo.transform.tag, "Sphere") == 0)
                    {
                        moveMode = eWidgetMode.Translate;
                    }
                    else
                    {
                        moveMode = eWidgetMode.Rotate;
                    }

                    wasDownOnControl = true;
                }
            }
            else if (eventData.pointerId == kRightMouse)
            {
                RaycastHit hitInfo;
                Ray ray = Camera.main.ScreenPointToRay(eventData.position);

                if (Physics.Raycast(ray, out hitInfo, 1000.0f, terrainLayer))
                {
                    if (Vector3.SqrMagnitude(hitInfo.point - this.transform.position) < Granularity)
                        return;

                    // experimenting with 'best' path
                    //if (FindBestPath(hitInfo.point, out finalPath))
                    if (FindPathAtCurrentSpeed(hitInfo.point, out finalPath))
                    {
                        SetPositionAndDirection(hitInfo.point, finalPath.GetEndDirectionVec3());

                        this.tileMoveable.CurrentPath = finalPath;

                        this.UpdatePathLines();

                        //well, it's technically true, since we teleported the widget to our mouse
                        wasDownOnControl = true;
                        moveMode = eWidgetMode.Rotate;

                        pathStartedAsReverse = finalPath.isReverse;
                    }
                }
            }
        }
    }

    public void OnPointerDrag(PointerEventData eventData)
    {
        if (wasDownOnControl && 
            (eventData.pointerId == kLeftMouse || eventData.pointerId == kRightMouse))
        {
            draggingHasOccured = true;
            RaycastHit hitInfo;
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);

            if (Physics.Raycast(ray, out hitInfo, 1000.0f, terrainLayer))
            {
                if (Vector3.SqrMagnitude(hitInfo.point - this.transform.position) < Granularity)
                    return;

                switch (moveMode)
                {
                    case eWidgetMode.Translate:
                        {
                            //if (FindBestPath(hitInfo.point, out finalPath))
                            if (FindPathAtCurrentSpeed(hitInfo.point, out finalPath))
                            {
                                SetPositionAndDirection(hitInfo.point, finalPath.GetEndDirectionVec3());
                                this.tileMoveable.CurrentPath = finalPath;
                                this.UpdatePathLines();
                            }
                        }
                        break;
                    case eWidgetMode.Rotate:
                        {
                            Vector2 destDir = new Vector2(hitInfo.point.x - this.transform.position.x,
                                                          hitInfo.point.z - this.transform.position.z).normalized;
                            
                            //if (FindBestPath(this.transform.position, destDir, out finalPath))
                            if (FindPathAtCurrentSpeed(this.transform.position, destDir, out finalPath))
                            {
                                this.tileMoveable.CurrentPath = finalPath;
                                SetPositionAndDirection(this.transform.position, finalPath.GetEndDirectionVec3());
                                this.UpdatePathLines();
                            }
                        }
                        break;
                }

            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerId == kLeftMouse &&
            draggingHasOccured == false &&
            tileMoveable != null &&
            finalPath != null)
        {
            
            RaycastHit hitInfo;
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out hitInfo, 1000.0f, widgetLayer))
            {
                this.tileMoveable.BeginMove();
                finalPath = null;
            }
        }
    }

    public void Select(TileMoveable newSelection)
    {
        if (newSelection != null)
        {
            this.ShowControls();
            //Unselect the old model?!

            this.tileMoveable = newSelection;
            this.tileMoveable.ghostTransform.gameObject.SetActive(true);

            this.ghostCollider = this.tileMoveable.ghostTransform.GetComponent<BoxCollider>();

            // todo:  Do I want to grab ownership /  become the parent of the ghostTransform,
            //           Then give it back after?
            this.tileMoveable.ghostTransform.parent = this.transform;
            

            if (this.tileMoveable.CurrentPath == null || this.tileMoveable.CurrentPath.path.Count == 0)
            {
                SetPositionAndDirection( tileMoveable.transform.position, tileMoveable.transform.forward);
                this.ClearPathLines();
            }
            else
            {
                UpdatePathLines();

                Vector3 finalPos;
                Vector3 finalDir;

                this.tileMoveable.CurrentPath.GetEndDirAndPos(out finalDir, out finalPos);

                Debug.Log("FinalPos:" + finalPos);

                SetPositionAndDirection(finalPos, finalDir);
            }
        }
        else
        {
            this.HideControls();
           // this.gameObject.SetActive(false);
        }
    }

    public void ClearPathLines()
    {
        this.canMoveLineRenderer.SetVertexCount(0);
        this.pastMoveLineRenderer.SetVertexCount(0);
    }
    public void UpdatePathLines()
    {
        MoveablePath.LineRenderArgs args = new MoveablePath.LineRenderArgs()
        {
             LineWidth = tileMoveable.ModelWidth,
             StartDistance = 0.0f,
             EndDistance = tileMoveable.CurrentMoveDistance
        };

        if (tileMoveable.CurrentPath != null)
        {
            this.tileMoveable.CurrentPath.RenderToLine(canMoveLineRenderer, args);

            float totalDistance = tileMoveable.CurrentPath.CalculateTotalDistance();

            if (totalDistance > tileMoveable.CurrentMoveDistance)
            {
                args.StartDistance = tileMoveable.CurrentMoveDistance;
                args.EndDistance = totalDistance;
                //TODO I relaly should cache the end distanec, i use it everywher.e
                this.tileMoveable.CurrentPath.RenderToLine(pastMoveLineRenderer, args);
            }
            else
            {
                this.pastMoveLineRenderer.SetVertexCount(0);
            }
        }
        else
        {
            this.canMoveLineRenderer.SetVertexCount(0);
			this.pastMoveLineRenderer.SetVertexCount(0);
        }
    }

    public void HideControls()
    {
        for (int i = 0; i < this.childParts.Length; ++i)
        {
            this.childParts[i].SetActive(false);
        }
    }

    public void ShowControls()
    {
        for (int i = 0; i < this.childParts.Length; ++i)
        {
            this.childParts[i].SetActive(true);
        }
    }

    public void Repath()
    {
        if (this.tileMoveable.CurrentPath != null)
        {
            DiscreteSpeedMoveable discreteMoveable = tileMoveable.GetComponent<DiscreteSpeedMoveable>();

            finalPath = null;
            bool bCouldUpdatePath = false;
            bool bNeedRepath = false;

            if (this.tileMoveable.transform.position == this.transform.position &&
                this.tileMoveable.transform.forward == this.transform.forward &&
                discreteMoveable.currentSpeed == tileMoveable.CurrentPath.speed)
            {
                bNeedRepath = false;
            }
            else 
            {
                bNeedRepath = true;
                if (this.tileMoveable.CurrentPath.isDirectional)
                {
                    // Directional search   
                    bCouldUpdatePath
                        = FindPathAtCurrentSpeed(this.transform.position, this.tileMoveable.CurrentPath.GetEndDirectionVec3(), out finalPath);
                }
                else
                {
                    // Non-Directional search
                    bCouldUpdatePath
                       = FindPathAtCurrentSpeed(this.transform.position, out finalPath);
                }
            }

            if (bNeedRepath)
            {
                if (!bCouldUpdatePath)
                {
                    SetPositionAndDirection(this.tileMoveable.transform.position, this.tileMoveable.transform.forward);
                }
                else
                {
                    SetPositionAndDirection(this.transform.position, finalPath.GetEndDirectionVec3());
                }
            }

            this.tileMoveable.CurrentPath = finalPath;
            this.UpdatePathLines();
        }
    }

    // Called by Broadcast
    public void DiscreteSpeedChanged(GameObject sender, System.Object data)
    {
        if (this.tileMoveable && sender == this.tileMoveable.gameObject)
        {
            Repath();
        }
    }

    public void TurnEnded(GameObject sender, System.Object data)
    {
        if (this.tileMoveable != null)
        {
            this.UpdatePathLines();
            //Repath();
        }
    }

    public void MoveEnded(GameObject sender, System.Object data)
    {
        if (this.tileMoveable && sender == this.tileMoveable.gameObject)
        {
            this.UpdatePathLines();
            //Repath();
        }
    }

    // TODO:  I should really just wrap that dubinCSCList into a class....
    private float TotalDistance(List<DubinCSC> path)
    {
        float distance = 0.0f;
        for(int i = 0; i < path.Count; ++i)
        {
            distance += path[i].totalLength;
        }
        return distance;
    }


    private void SetPositionAndDirection(Vector3 pos, Vector3 dir)
    {
        this.transform.position = pos;
        this.transform.forward = dir;

        this.tileMoveable.ghostTransform.position = this.transform.position;
        this.tileMoveable.ghostTransform.rotation = this.transform.rotation;

        BoxCollider boxCollider = tileMoveable.GetComponent<BoxCollider>();

        this.childParts[1].transform.localPosition = Vector3.forward * (boxCollider.size.z / 2.0f) + Vector3.up;
    }

    private bool DoFindPath(TileGrid.FindPathArgs args, DiscreteSpeedMoveable discreteMoveable, out MoveablePath pathForSpeed)
    {
        List<DubinCSC> path = null;
        pathForSpeed = null;
        bool pathfound = false;

        if (TileGrid.Instance().FindPath(args, out path))
        {
            pathfound = true;

            pathForSpeed = new MoveablePath()
            {
                path = path,
                turnRadius = discreteMoveable.CurrentTurnRadius,
                isReverse = discreteMoveable.currentSpeed == Speed.Reverse,
                isDirectional = args.IsDirectionalPath,
                moveRateCost = 1.0f,
                speed = discreteMoveable.currentSpeed
            };
        };

         return pathfound;
    }

    bool FindPathAtCurrentSpeed(Vector3 dest, Vector3 destDir, out MoveablePath pathForSpeed)
    {
        DiscreteSpeedMoveable discreteMoveable = tileMoveable.GetComponent<DiscreteSpeedMoveable>();
        
        float dir = discreteMoveable.currentSpeed == Speed.Reverse ? -1.0f : 1.0f;
        
        // Note: We start out testing reverse first, hence the negatives
        var args = new TileGrid.FindPathArgs()
        {
            Fwd = dir * tileMoveable.transform.forward,
            Right = dir * tileMoveable.transform.right,
            TurnRadius = discreteMoveable.CurrentTurnRadius,
            Pos = tileMoveable.transform.position,
            Dest = dest,
            DestDir = destDir,
            BoxCollider = tileMoveable.boxCollider,
            IsDirectionalPath = true
        };

        return DoFindPath(args, discreteMoveable, out pathForSpeed);
    }

    bool FindPathAtCurrentSpeed(Vector3 dest, out MoveablePath pathForSpeed)
    {
        DiscreteSpeedMoveable discreteMoveable = tileMoveable.GetComponent<DiscreteSpeedMoveable>();

        float dir = discreteMoveable.currentSpeed == Speed.Reverse ? -1.0f : 1.0f;
        var args = new TileGrid.FindPathArgs()
        {
            Fwd = dir * tileMoveable.transform.forward,
            Right = dir * tileMoveable.transform.right,
            TurnRadius = discreteMoveable.CurrentTurnRadius,
            Pos = tileMoveable.transform.position,
            Dest = dest,
            BoxCollider = tileMoveable.boxCollider,
        };

        return DoFindPath(args, discreteMoveable, out pathForSpeed);
    }

    //bool FindPath(Vector3 dest, Vector3 destDir, out MoveablePath bestMoveablePath)
    //{

    //}

    //bool FindPath(Vector3 dest, out MoveablePath bestMoveablePath)
    //{

    //}

    // TODO: These functions are a bit out of place, steps all over other territory.
    // I should probably move'em to a discrete moveable
    bool DoFindBestPath(DiscreteSpeedMoveable discreteMoveable, TileGrid.FindPathArgs args, out MoveablePath bestMoveablePath, bool flipBackAfterReverse)
    {
        bool pathFound = false;
        List<DubinCSC> path;
        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        List<DubinCSC> bestPath;

        // reverse
        // neutral
        // combat speed
        // full

        // check reverse manually, since it has slightly different Args
        pathFound |= TileGrid.Instance().FindPath(args, out bestPath);
        if (pathFound)
        {
            bestDistance = TotalDistance(bestPath) * discreteMoveable.SpeedCostArray[0];
        }

        // flip the fwd/right args for all forward movement
        if (flipBackAfterReverse)
        {
            args.Fwd = -args.Fwd;
            args.Right = -args.Right;
        }

        for (int i = 1; i < discreteMoveable.SpeedCostArray.Length; ++i)
        {
            // check next
            args.TurnRadius = discreteMoveable.TurnRadiusArray[i];

            if (TileGrid.Instance().FindPath(args, out path))
            {
                float nextDistance = TotalDistance(path) * discreteMoveable.SpeedCostArray[i];
                if (nextDistance < bestDistance)
                {
                    bestDistance = nextDistance;
                    bestIndex = i;
                    bestPath = path;
                }
                pathFound = true;
            }
        }

        bestMoveablePath = new MoveablePath()
        {
            path = bestPath,
            turnRadius = discreteMoveable.TurnRadiusArray[bestIndex],
            isReverse = bestIndex == 0,
            isDirectional = args.IsDirectionalPath,
            moveRateCost =discreteMoveable.SpeedCostArray[bestIndex],
            speed = (Speed)bestIndex
        };

        return pathFound;
    }

    bool FindBestPath(Vector3 dest, Vector3 destDir, out MoveablePath bestMoveablePath)
    {
        DiscreteSpeedMoveable discreteMoveable = tileMoveable.GetComponent<DiscreteSpeedMoveable>();

        // Note: For directional paths, we do NOT flip for reverse movement.
        var args = new TileGrid.FindPathArgs()
        {
            Fwd = -tileMoveable.transform.forward,
            Right = -tileMoveable.transform.right,
            TurnRadius = discreteMoveable.TurnRadiusArray[0],
            Pos = tileMoveable.transform.position,
            Dest = dest,
            DestDir = destDir,
            BoxCollider = tileMoveable.boxCollider,
            IsDirectionalPath = true
        };

        return DoFindBestPath(discreteMoveable, args, out bestMoveablePath, true);
    }
    
    bool FindBestPath(Vector3 dest, out MoveablePath bestMoveablePath)
    {
        DiscreteSpeedMoveable discreteMoveable = tileMoveable.GetComponent<DiscreteSpeedMoveable>();

        // Note: We start out testing reverse first, hence the negatives
        var args = new TileGrid.FindPathArgs()
        {
            Fwd = -tileMoveable.transform.forward,
            Right = -tileMoveable.transform.right,
            TurnRadius = discreteMoveable.TurnRadiusArray[0],
            Pos = tileMoveable.transform.position,
            Dest = dest,
            BoxCollider = tileMoveable.boxCollider
        };

        return DoFindBestPath(discreteMoveable, args, out bestMoveablePath, true);
    }
}
