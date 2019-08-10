 using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Priority_Queue;

public struct GridPoint2D
{
	public int x;
	public int y;
}

public class DubinTableEntry
{
    public List<int> offsets1D = new List<int>();
	public DubinCSC OffsetDubin;
	public DirectionalCircle leftOffsetDC;
	public DirectionalCircle rightOffsetDC;
}

public struct DubinTableKey
{
    public DubinTableKey(BoxCollider collider)
    {
        Center = collider.center;
        Size = collider.size;
    }

    Vector3 Center { get; set; }
    Vector3 Size { get; set; }
}

public class DubinRadiusTable
{
	private class DubinTableStorage
	{
		public DubinTableEntry[,,] entryArray = new DubinTableEntry[8,8,48];
	}

        

	private Dictionary<int, DubinTableStorage> storagePerCollider = new Dictionary<int, DubinTableStorage> ();

	public DubinRadiusTable(float radius)
	{
		turnRadius = radius;
		
		CreateHeuristicTables();
		// c
	}
	
	public readonly float turnRadius;
//private DubinTableEntry[,,] entryArray = new DubinTableEntry[8,8,48];
		
	public DubinTableEntry GetDubinEntry(TileDirection orgDirection, int offsetTileIdx, TileDirection offsetDirection, BoxCollider boxCollider)
	{

        //DubinTableKey key = new DubinTableKey(boxCollider);
        // TODO: 
        // this is where things fall down, I should make a 'key' based upon width of the box collider
        if (!storagePerCollider.ContainsKey(boxCollider.GetInstanceID()))
		{
			storagePerCollider.Add(boxCollider.GetInstanceID(), new DubinTableStorage());
            Debug.Log("Creating new DubinTableStorage!");
		}
		DubinTableStorage storage = storagePerCollider[boxCollider.GetInstanceID()];

		if (storage.entryArray[(int)orgDirection, (int)offsetDirection, offsetTileIdx] != null)
		{
			return storage.entryArray[(int)orgDirection,(int)offsetDirection, offsetTileIdx];
		}

        // TODO:  Something really weird going on, we're creating more entries when we shouldn't be.  Not sure if it's GC fucking with me?  
            
        // doesn't yet exist, lets add it to the table
       // Debug.Log("Adding a new Entry to the Dubin Table: " + string.Format("{0} : {1}, {2}, {3}", boxCollider.gameObject.name, orgDirection, offsetDirection, offsetTileIdx), boxCollider.gameObject);

		int tileOffset = TileGrid.Instance().GetTileOffsetForTileOffsetIdx(offsetTileIdx);
		int gridWidth = TileGrid.Instance().Width;
						
		//Construct the destination circles for this tile direction
		// pretend we are at the center of grid for the sake of it.
		// It's terrible really, and will fail for small grids
		int centerIdx = gridWidth / 2 + (TileGrid.Instance().Height / 2) * gridWidth;
		//TileSquare centerSq = TileGrid.Instance().GetSquare(centerIdx);

        Vector2 startPt = TileGrid.Instance().GetTilePointFromIndex(centerIdx);
        Vector2 destPt = TileGrid.Instance().GetTilePointFromIndex(centerIdx + tileOffset); 
			
		//Small note, these could be all optimized, since we know the directions, we technically know the angles (which we compute inside here)
		Vector2 offset = TileCoordinates.GetLeftCircleOffset(orgDirection);
		DirectionalCircle startDCLeft  = new DirectionalCircle(new Vector2(startPt.x + offset.x * turnRadius, 
																			startPt.y + offset.y * turnRadius), startPt, false);
			
		DirectionalCircle startDCRight = new DirectionalCircle(new Vector2(startPt.x - offset.x * turnRadius, 
																  		    startPt.y - offset.y * turnRadius), startPt, true);
			
		offset = TileCoordinates.GetLeftCircleOffset(offsetDirection);
		DirectionalCircle destDCLeft  = new DirectionalCircle(new Vector2(destPt.x + offset.x * turnRadius, 
																    		destPt.y + offset.y * turnRadius), destPt, false);
						
		DirectionalCircle destDCRight = new DirectionalCircle(new Vector2(destPt.x - offset.x * turnRadius, 
																  		    destPt.y - offset.y * turnRadius), destPt, true);
			
		DubinTableEntry entry = new DubinTableEntry();
		// Find all Dubins to this neighbor tile
		//DubinCSC.FindDubins(startDCLeft, startDCRight, destDCLeft, destDCRight, startPt, destPt, turnRadius, tileDubins);
        entry.OffsetDubin =  DubinCSC.FindDubin(startDCLeft, startDCRight, startPt, destPt, turnRadius, destDCLeft, destDCRight);
			
		if(entry.OffsetDubin.IsValidDubin())
		{
			//TODO replace this function with a TileOffsets version for cleaner implementation
			HashSet<int> tilesUnder = TileGrid.Instance().TilesUnderDubin(entry.OffsetDubin, turnRadius, boxCollider);

            foreach (int i in tilesUnder)
			{
                entry.offsets1D.Add(i - centerIdx);
                //entry.offsets1D.Add((tilesUnder[i].x - centerSq.x) + (tilesUnder[i].y - centerSq.y) * gridWidth);
			}

            entry.offsets1D.Sort();  // sort for a more linear array access

			// Convert the dubin to be starting at 0,0 in world space, for easier translation later
			entry.OffsetDubin = DubinCSC.Translate(entry.OffsetDubin, -startPt);
			    
            entry.leftOffsetDC = destDCLeft;
            entry.leftOffsetDC.center -= startPt;
				
            entry.rightOffsetDC = destDCRight;
            entry.rightOffsetDC.center -= startPt;
              
		}
		
		storage.entryArray[(int)orgDirection, (int)offsetDirection, offsetTileIdx] = entry;
		
		return entry;
	}
	
	const int kHeuristicTableSize = 10;
	
	// This is the table for when we don't care about the end value
	double[,,] heuristicValueTable = new double [(int)TileDirection.NUM_DIRS, 2*kHeuristicTableSize, 2*kHeuristicTableSize];
	double[,,,] heuristicWithEndDirectionValueTable = new double [(int)TileDirection.NUM_DIRS, 2*kHeuristicTableSize, 2*kHeuristicTableSize, (int)TileDirection.NUM_DIRS];
	private void CreateHeuristicTables()
	{
		int gridWidth = TileGrid.Instance().Width;
		//hackery, going from the center tile
		int centerIdx = gridWidth / 2 + (TileGrid.Instance().Height / 2) * gridWidth;
		Vector2 startPt = TileGrid.Instance().GetTilePointFromIndex(centerIdx);
		
		DirectionalCircle dummyCircle = new DirectionalCircle(Vector2.zero, false);
		
		for(TileDirection startDir = TileDirection.N; startDir < TileDirection.NUM_DIRS; ++startDir)
		{
			
			Vector2 offset = TileCoordinates.GetLeftCircleOffset(startDir);
			DirectionalCircle startDCLeft  = new DirectionalCircle(new Vector2(startPt.x + offset.x * turnRadius, 
																			   	startPt.y + offset.y * turnRadius), startPt, false);
			
			DirectionalCircle startDCRight = new DirectionalCircle(new Vector2(startPt.x - offset.x * turnRadius, 
																  		        startPt.y - offset.y * turnRadius), startPt, true);
			
			for(int j = -kHeuristicTableSize; j < kHeuristicTableSize; ++j)
			{
				for(int i = -kHeuristicTableSize; i < kHeuristicTableSize; ++i)
				{
					int destIdx = centerIdx +  i + j * TileGrid.Instance().Width;

                    Vector2 destPt = TileGrid.Instance().GetTilePointFromIndex(destIdx);
					
					//for(TileDirection endDir = TileDirection.N; startDir < TileDirection.NUM_DIRS; ++startDir)
					
					DubinCSC dubin = DubinCSC.FindDegenerateDubin(startDCLeft, startDCRight, startPt, destPt, turnRadius, dummyCircle,dummyCircle);
					
				    heuristicValueTable[(int)startDir, i + kHeuristicTableSize, j + kHeuristicTableSize] = dubin.totalLength;
					
					for(TileDirection endDir = TileDirection.N; endDir < TileDirection.NUM_DIRS; ++endDir)
					{
						
						offset = TileCoordinates.GetLeftCircleOffset(endDir);
						DirectionalCircle destDCLeft  = new DirectionalCircle(new Vector2(destPt.x + offset.x * turnRadius, 
																    		destPt.y + offset.y * turnRadius), destPt, false);
						
						DirectionalCircle destDCRight = new DirectionalCircle(new Vector2(destPt.x - offset.x * turnRadius, 
																  		    destPt.y - offset.y * turnRadius), destPt, true);
						
						dubin = DubinCSC.FindDegenerateDubin(startDCLeft, startDCRight, startPt, destPt, turnRadius, destDCLeft,destDCRight);
						heuristicWithEndDirectionValueTable[(int)startDir, i + kHeuristicTableSize, j + kHeuristicTableSize, (int)endDir] = dubin.totalLength;
					}
				}
			}
		}
	}
	
	public double GetHValue(TileDirection startDir, TileDirection endDir,  int destOffsetX, int destOffsetY)
	{
		//first check that the offset is legal, if it's not we have to cap the offset, then add the additional distance from the offset
		return heuristicWithEndDirectionValueTable[(int)startDir, destOffsetX + kHeuristicTableSize, destOffsetY + kHeuristicTableSize, (int)endDir];
	}

    public double GetHValue(TileDirection startDir, int destOffsetX, int destOffsetY)
	{
		return 0.0f;
	}
}
public class DubinTable
{
	private Dictionary<float, DubinRadiusTable> radiusTables = new Dictionary<float, DubinRadiusTable>();
	
	public DubinTable()
	{

	}
	
    public DubinRadiusTable GetTable(float turnRadius)
	{
		DubinRadiusTable table;
		if(radiusTables.TryGetValue(turnRadius, out table))
			return table;
		
		table = new DubinRadiusTable(turnRadius);
		radiusTables.Add(turnRadius, table);
		
		return table;
		
	}
}
	
//TODO:  Clean this up
public class TileVisit : PriorityQueueNode
{
	public TileVisit prevTile;
	public DubinCSC path;
	
	// There is some overlap here.... could use optimization
	public Vector2 ptStart; // technically could save the index...
	public DirectionalCircle orgLeft;
	public DirectionalCircle orgRight;
	
	public DubinCSC goalDubin;
	
	public TileDirection direction;
	
    public float CalculatePriority()
    {
        const float kGPreference = 1.0f;  // I'm preferring smaller paths over arbitrarily pointing towards the goal
        return goalDubin.GetHValue() + kGPreference * path.GetHValue();
    }

	public List<DubinCSC> GetAsDubinList()
	{
		Stack<DubinCSC> stack = new Stack<DubinCSC>();
		TileVisit w = this;
		
		while(w != null)
		{
			if(w.path.IsValidDubin())
			{
				//list.Insert(list.Count, w.path);
				stack.Push(w.path);
			}
			
			w = w.prevTile;
		}
		
		List<DubinCSC> list = new List<DubinCSC>(stack.Count);
		while(stack.Count > 0)
			list.Add(stack.Pop());
		
		return list;
	}


    public void InitializeTile(TileDirection newDirection, DirectionalCircle aLeft, DirectionalCircle aRight, TileVisit aPrevTile, DubinCSC aPath, Vector2 aStartPoint)
    {
        direction = newDirection;
        orgLeft = aLeft;
        orgRight = aRight;
        prevTile = aPrevTile;
        path = aPath;
        ptStart = aStartPoint;

        goalDubin = DubinCSC.NullDubin;
    }

    public void InitializeTile(TileDirection newDirection, DirectionalCircle aLeft, DirectionalCircle aRight, TileVisit aPrevTile, DubinCSC aPath, Vector2 aStartPoint, DubinCSC aGoalDubin)
    {
        direction = newDirection;
        orgLeft = aLeft;
        orgRight = aRight;
        prevTile = aPrevTile;
        path = aPath;
        ptStart = aStartPoint;

        goalDubin = aGoalDubin;
    }
}

public class TileCoordinates
{
	static readonly IList<Vector2> leftOffsets = new ReadOnlyCollection<Vector2>
	(new[] {
            new Vector2(-1.0f,  0.0f), // N
			new Vector2(-1.0f,  1.0f) / Mathf.Sqrt(2.0f), // NE
			new Vector2( 0.0f,  1.0f), // E
			new Vector2( 1.0f,  1.0f) / Mathf.Sqrt(2.0f), // SE
			new Vector2( 1.0f,  0.0f), // S
			new Vector2( 1.0f, -1.0f) / Mathf.Sqrt(2.0f), // SW
		    new Vector2( 0.0f, -1.0f), // W
			new Vector2(-1.0f, -1.0f) /  Mathf.Sqrt(2.0f), // NW
        });
	
	static public Vector2 GetLeftCircleOffset(TileDirection d)
	{
		return leftOffsets[(int)d];
	}
}

public enum TileDirection : int
{
	N,// 0
	NE, 	
	E,
	SE,
	S,
	SW,
	W,
	NW,
	NUM_DIRS,
	CUSTOM
}

// TODO: Kill TileSquare
public class TileSquare
{
	public GameObject debugPrimitive;
}

public class TileGrid : MonoBehaviour
{
	int[] dir24;
	int dir24Length = 0;
	
	int[] dir48;
	int dir48Length;
	
	//Singleton
	static TileGrid s_instance = null;
	static public TileGrid Instance() { return s_instance;}
	
	public int Width = 10;
	public int Height = 10;
	public float TileSize = 10;
	public Terrain terrain;

    public uint MaxQueueSize;

    public float Density = 20.0f;
	
	public Material debugHit;
	public Material debugCollision;
	public Material debugPath;
	public Material debugNode;
	public Material debugSprint;
	public Material debugSlow;
	
	public LayerMask terrainLayer;
    public GameObject CubeRoot;
	
	public bool bUse48Neighbors = true;
    public bool HighlightLineTraveled = false;

    List<TileSquare> debugSelectedSquares = new List<TileSquare>();

    PriorityQueueNode.HeapPriorityQueue<TileVisit> priorityQueue;
    TileVisit[] tileVisitPool;

    int searchPass = 0;
    int[] visitedTiles; // if a tile has been visited or not for this search pass
    private TileSquare[] squares;
    private bool[] isBlockedGrid;
    //private Vector2[] tilePosition2Grid;
    DubinTable dubinTable = new DubinTable();
    Vector3 static_position;

    #region TileCoordinateHelpers
    int GetXTileIndex(float worldX)
    {
        return Mathf.RoundToInt((worldX - static_position.x) / this.TileSize);
    }

    int GetYTileIndex(float worldZ)
    {
        return Mathf.RoundToInt((worldZ - static_position.z) / this.TileSize);
    }

    public int GetTileOffsetForTileOffsetIdx(int i)
	{
		if(bUse48Neighbors)
			return dir48[i];
		else
			return dir24[i];
	}
	
	public bool IsValidTile( int x, int y)
	{
		return ( x >= 0 && x < this.Width && y >= 0 && y < this.Height);
	}

    int GetTileIndex(Vector2 worldPos)
    {
        int x = GetXTileIndex(worldPos.x);
        int y = GetYTileIndex(worldPos.y);

        if (IsValidTile(x, y))
        {
            return x + y * this.Width;
        }

        return -1;
    }

    int GetTileIndex3(Vector3 worldPos)
    {
        int x = GetXTileIndex(worldPos.x);
        int y = GetYTileIndex(worldPos.z);

        if (IsValidTile(x, y))
        {
            return x + y * this.Width;
        }

        return -1;
    }

    public Vector2 GetTilePointFromIndex(int index)
    {
        int x = index % Width;
        int y = index / Width;

        return GetTilePointFromIndices(x, y);
    }

    public Vector3 GetTilePointFromIndex3(int index)
    {
        int x = index % Width;
        int y = index / Width;
        
        return new Vector3(
            static_position.x + x * TileSize,
            0.0f,
            static_position.z + y * TileSize
            );
    }

    public Vector2 GetTilePointFromIndices(int x, int y)
    {
        return new Vector2(
         static_position.x + x * TileSize,
         static_position.z + y * TileSize
         );
    }

    bool IsTileBlocked(Vector2 worldPos)
    {
        int x = GetXTileIndex(worldPos.x);
        int y = GetYTileIndex(worldPos.y);

        if (IsValidTile(x, y))
        {
            return this.isBlockedGrid[x + y * this.Width];
        }

        return true;
    }

	bool IsValidTile(int idx)
	{
		return (idx >= 0 && (idx < (this.Width * this.Height)) );
	}

    public TileSquare GetSquare(int x, int y)
    {
        if (!IsValidTile(x, y))
        {
            Debug.Log("HOW");
        }
        return squares[x + y * Width];
    }

    public TileSquare GetSquare(int idx)
    {

        return squares[idx];

    }

    public TileSquare GetTile(Vector3 worldPos)
    {
        int x = GetXTileIndex(worldPos.x);
        int y = GetYTileIndex(worldPos.z);

        if (IsValidTile(x, y))
        {
            return this.squares[x + y * this.Width];
        }

        return null;
    }

    TileSquare GetTile(Vector2 worldPos)
    {
        int x = GetXTileIndex(worldPos.x);
        int y = GetYTileIndex(worldPos.y);

        if (IsValidTile(x, y))
        {
            return this.squares[x + y * this.Width];
        }

        return null;
    }

    //Note this is the floating point version, so 1.25 is 1/4 of the way onto the 1st tile
    Vector2 WorldToTile(Vector3 worldPos)
    {
        return new Vector2(worldPos.x - this.transform.position.x, worldPos.z - this.transform.position.z) / this.TileSize;
    }

    bool TileToWorld(int ix, int iy, out float x, out float y)
    {
        x = ix * TileSize + this.transform.position.x;
        y = iy * TileSize + this.transform.position.z;

        return true;
    }
    #endregion

    void Awake()
	{
		if(s_instance == null)
		{
			s_instance = this;
            s_instance.MakeGrid();
		}

        UnityEngine.Profiling.Profiler.maxNumberOfSamplesPerFrame = 8000000;
    }
	
	public bool AddToGrid(TileMoveable moveable)
	{
        int idx = moveable.tileX + moveable.tileY * this.Width;
		if(IsValidTile(idx) && !isBlockedGrid[idx])
		{
            moveable.transform.position = GetTilePointFromIndex3(idx);
			return true;
		}
		
		Debug.Log("Trying to set moveable to invalid tile");
		return false;
	}
	
    public bool SetMoveablePosition(TileMoveable moveable, int newX, int newY)
    {
        //if(IsValidTile(moveable.tileX, moveable.tileY))
        //{
        //    // TODO:  Remove 'blocked' status from tile / remove from 'occupants' or whatever
        //}
        int newIdx = newX + newY + this.Width;

        if (IsValidTile(newIdx) && !isBlockedGrid[newIdx])
		{
            moveable.transform.position = GetTilePointFromIndex3(newIdx);
            moveable.tileX = newX;
            moveable.tileY = newY;
			return true;
		}
        else
		{
            Debug.Log("Trying to set moveable to invalid tile");
		    return false;
        }
    }

	void MakeGrid()
	{
        static_position = this.transform.position; // weirdly Transform.get_position() & component.get_transform were taking noticeable amount of time in a profiler

        Random.seed = 10;
		
		dir24 = new int[24];
		for(int y = -2; y <= 2; ++y)
		{
			for(int x = -2; x <= 2; ++x)
			{
				if(x == 0 && y == 0)
					continue;
				
				dir24[dir24Length++] = x + y * this.Width;
			}
		}
		
		dir48 = new int[48];
		for(int y = -3; y <= 3; ++y)
		{
			for(int x = -3; x <= 3; ++x)
			{
				if(x == 0 && y == 0)
					continue;
				
				dir48[dir48Length++] = x + y * this.Width;
			}
		}

        // TODO: this maxqueue size is insane
        //MaxQueueSize = (uint) (this.Width * Height * (this.bUse48Neighbors ? 48 : 24) * 16 + 100);
        MaxQueueSize = (uint) (this.Width * Height * (int)TileDirection.NUM_DIRS);
        priorityQueue = new PriorityQueueNode.HeapPriorityQueue<TileVisit>(MaxQueueSize);
        tileVisitPool = new TileVisit[Width * Height * (int)TileDirection.NUM_DIRS];
        visitedTiles = new int[Width * Height * (int)TileDirection.NUM_DIRS];

        for (int i = 0; i < tileVisitPool.Length; ++i)
        {
            tileVisitPool[i] = new TileVisit();
        }

        System.Array.Clear(visitedTiles, 0, visitedTiles.Length);
        

        squares = new TileSquare[Width * Height];
        isBlockedGrid = new bool[Width * Height];
        //tilePosition2Grid = new Vector2[Width * Height];

        System.Array.Clear(isBlockedGrid, 0, isBlockedGrid.Length);

		for(int j = 0; j < Height; ++j)
		{
			for(int i = 0; i < Width; ++i)
			{
                TileSquare newSq = new TileSquare();
				float chance = Random.value  * 100.0f;

                float chanceOfBlock = 100.0f - this.Density;
                if (chance > chanceOfBlock)
                {
                    isBlockedGrid[i + j * Width] = true;
                }

                if (i == 1 && j == 1)
                {
                    isBlockedGrid[i + j * Width] = true; //hackery cheat, since I'm adding the car to the grid at 1 1
                }
				
				if(isBlockedGrid[i + j * Width])
				{
                    // TODO: move debug primitive storage elsewhere, dictionary maybe?
					newSq.debugPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    newSq.debugPrimitive.transform.parent = this.CubeRoot.transform;
                    newSq.debugPrimitive.transform.position = new Vector3(i * TileSize + static_position.x, 0, j * TileSize + static_position.z);
                    newSq.debugPrimitive.transform.localScale = new Vector3(this.TileSize, this.TileSize, this.TileSize);
					newSq.debugPrimitive.GetComponent<Renderer>().material = debugCollision;
                    newSq.debugPrimitive.layer = LayerMask.NameToLayer("BlockLayer");
				}
				
				squares[i + j* Width] = newSq;
                //tilePosition2Grid[i + j * Width] = new Vector2(i * TileSize + static_position.x, j * TileSize + static_position.z);
            }
		}

        //for (int y = 0; y < Height; ++y)
        //{
        //    for (int x = 0; x < Width; ++x)
        //    {
        //        int tileIndex = this.GetTileIndex(tilePosition2Grid[x + y * Width]);

        //        int ax = tileIndex % Width;
        //        int ay = tileIndex / Width;

        //        if(ay != y || 
        //           ax != x)
        //        {
        //            Debug.LogError("Fucked Up Intgers");
        //        }

        //        Vector2 calcedPoint  = this.GetTilePointFromIndex(x + y * Width);

        //        if(calcedPoint.x != tilePosition2Grid[x + y * Width].x || 
        //           calcedPoint.y != tilePosition2Grid[x + y * Width].y)
        //        {
        //            Debug.LogError("Fucked Up WorldPos");
        //        }
        //    }
        //}


        //for(int i = 0; i < this.isBlockedGrid.Length; ++i)
        //{
        //    if (isBlockedGrid[i])
        //    {
        //        int x = i % Width;
        //        int y = i / Width;

        //        var debugPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //        debugPrimitive.transform.parent = this.CubeRoot.transform;
        //        debugPrimitive.transform.position = new Vector3(x * TileSize + static_position.x, 1.0f, y * TileSize + static_position.z);
        //        debugPrimitive.transform.localScale = new Vector3(this.TileSize, this.TileSize, this.TileSize) * .9f;
        //        debugPrimitive.GetComponent<Renderer>().material = debugNode;
        //        debugPrimitive.layer = LayerMask.NameToLayer("BlockLayer");
        //    }
        //}
    }
	
 

    // crappy helper function just to avoid the same logic in all the line/arc checks
	bool DEBUG_TilesUnderArc(Dictionary<int, TileSquare> tilesVisited, Arc arc, float turnRadius, bool stopOnBlock)
	{
		if (Mathf.Approximately (arc.length, 0.0f))
			return true;
		// Note this is bog slow and probably could use heavy optimization (Wu's circle algorithm?)
		float distInc = this.TileSize / 5.0f;
		
		float tInc = distInc / arc.length;
		float rotDir = arc.circle.clockwise == true ? -1.0f : 1.0f;
		for (float t = 0.0f; t <= 1.0f; t += tInc) 
		{

			float angle = arc.startTheta + rotDir * (Mathf.Lerp(0, arc.length, t) / turnRadius);
			Vector3 nextPos = new Vector3(arc.circle.center.x + turnRadius * Mathf.Cos(angle),
															    1.0f,
										  arc.circle.center.y + turnRadius * Mathf.Sin(angle));
	
            //!isBlockedGrid[idx]

            int idx = GetTileIndex3(nextPos);
			if(stopOnBlock && (idx == -1 || isBlockedGrid[idx]))
			{
				return false;
			}


            if (idx != -1)
			{
                if (!tilesVisited.ContainsKey(idx))
				{
                   // tilesVisited.Add(idx, sq);
				}
			}

		}

//		float distInc = arc.length / 300.0f; //hackery
//		float distTraveled = 0.0f;
//		
//		if(arc.length > distInc)
//		{
//			float rotDir = arc.circle.clockwise == true ? -1.0f : 1.0f;
//			while(distTraveled < arc.length)
//			{
//				float angle = arc.startTheta + rotDir * (distTraveled / turnRadius);
//				Vector3 nextPos = new Vector3(arc.circle.center.x + turnRadius * Mathf.Cos(angle),
//																    1.0f,
//											  arc.circle.center.y + turnRadius * Mathf.Sin(angle));
//
//				distTraveled += distInc;
//				TileSquare sq = GetTile(nextPos);
//				
//				if(stopOnBlock && sq == null)
//					return false;
//				
//				if(sq != null)
//				{
//					int index = sq.x + sq.y * this.Width;
//					if(!tilesVisited.ContainsKey(index))
//					{
//						if(stopOnBlock && sq.isBlocked)
//							return false;
//						
//						tilesVisited.Add(index, sq);
//					}
//				}
//			}
//		}
		
		return true;
	}

    const float  kTileDiv = 5.0f; // originally 5
    bool TilesUnderArc(HashSet<int> tileIdxsVisited, Arc arc, float turnRadius, BoxCollider boxCollider)
	{
        if (MyMath.IsNearZero(arc.length)) // a nearzero arc is just clear
			return true;
		// Note this is bog slow and probably could use heavy optimization (Wu's circle algorithm?)
		float distInc = this.TileSize / kTileDiv;
		
		float tInc = distInc / arc.length;
		float rotDir = arc.circle.clockwise == true ? -1.0f : 1.0f;

		float halfWidth = boxCollider.gameObject.transform.localScale.x * boxCollider.size.x / 2.0f;
        float halfLength = boxCollider.gameObject.transform.localScale.z * boxCollider.size.z / 2.0f;

        for (float t = 0.0f; t <= (1.0f + tInc); t += tInc) 
		{
            // 
            //  Given a box, what points should we sample
            //  for now, I'm going with
            //  the corners of the box and the center
            //
            //
            //

			float angle = arc.startTheta + rotDir * (Mathf.Lerp(0, arc.length, t) / turnRadius);
			float cosAngle = Mathf.Cos(angle);
			float sinAngle = Mathf.Sin(angle);

            // determine the forward vector
            Vector2 dirVec = new Vector2(-sinAngle, cosAngle) * rotDir * halfLength;
            //Vector2 leftVec = new Vector2((turnRadius + halfWidth) * cosAngle, (turnRadius + halfWidth) * sinAngle);


			Vector2 centerPos = new Vector2(arc.circle.center.x + turnRadius * cosAngle,
			                                arc.circle.center.y + turnRadius * sinAngle);

            Vector2 centerForwardPos = centerPos + dirVec;
            Vector2 leftPos = new Vector2(arc.circle.center.x + (turnRadius + halfWidth) * cosAngle,
                                          arc.circle.center.y + (turnRadius + halfWidth) * sinAngle);
            Vector2 rightPos = new Vector2(arc.circle.center.x + (turnRadius - halfWidth) * cosAngle,
                                           arc.circle.center.y + (turnRadius - halfWidth) * sinAngle);

            Vector2 upperLeftPos  = leftPos  + dirVec;
            Vector2 lowerLeftPos  = leftPos  - dirVec;
            Vector2 lowerRightPos = rightPos - dirVec;
            Vector2 upperRightPos = rightPos + dirVec;

            Vector2[] positionsToCheck = new Vector2[] { centerPos, centerForwardPos, upperLeftPos, upperRightPos, lowerLeftPos, lowerRightPos };

            for(int i = 0; i < positionsToCheck.Length; ++i)
            {
                int idx = GetTileIndex(positionsToCheck[i]);

                //if (stopOnBlock && (idx == -1 || isBlockedGrid[idx]))
                //{
                //    return false;
                //}

                if(idx != -1)
                {
                    tileIdxsVisited.Add(idx);
                }
            } 

		}
		
		return true;
	}

    Vector2[] positionsToCheck = new Vector2[8];
    bool IsArcClear(Arc arc, float turnRadius, BoxCollider boxCollider)
    {
        if (MyMath.IsNearZero(arc.length))
            return true;
        // Note this is bog slow and probably could use heavy optimization (Wu's circle algorithm?)
        float distInc = this.TileSize / kTileDiv;

        float tInc = distInc / arc.length;
        float rotDir = arc.circle.clockwise == true ? -1.0f : 1.0f;

        float halfWidth = boxCollider.gameObject.transform.localScale.x * boxCollider.size.x / 2.0f;
        float halfLength = boxCollider.gameObject.transform.localScale.z * boxCollider.size.z / 2.0f;

        for (float t = 0.0f; t <= (1.0f + tInc); t += tInc)
        {
            // 
            //  Given a box, what points should we sample
            //  for now, I'm going with
            //  the corners of the box and the center
            //
            //
            //

            float angle = arc.startTheta + rotDir * (Mathf.Lerp(0, arc.length, t) / turnRadius);
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);

            // determine the forward vector
            Vector2 dirVec = new Vector2(-sinAngle, cosAngle) * rotDir * halfLength;

            // centerPos
            positionsToCheck[0] = new Vector2(arc.circle.center.x + turnRadius * cosAngle,
                                              arc.circle.center.y + turnRadius * sinAngle);
            // centerForwardPos
            positionsToCheck[1] = positionsToCheck[0] + dirVec;

            // leftPos
            positionsToCheck[2] = new Vector2(arc.circle.center.x + (turnRadius + halfWidth) * cosAngle,
                                              arc.circle.center.y + (turnRadius + halfWidth) * sinAngle);

            // RightPos
            positionsToCheck[3] = new Vector2(arc.circle.center.x + (turnRadius - halfWidth) * cosAngle,
                                              arc.circle.center.y + (turnRadius - halfWidth) * sinAngle);
            
            positionsToCheck[4] = positionsToCheck[2] + dirVec; // upperLeftPos
            positionsToCheck[5] = positionsToCheck[2] - dirVec; // lowerLeftPos
            positionsToCheck[6] = positionsToCheck[3] - dirVec; // lowerLeftPos
            positionsToCheck[7] = positionsToCheck[3] + dirVec; // upperRightPos

            for (int i = 0; i < positionsToCheck.Length; ++i)
            {
                if (IsTileBlocked(positionsToCheck[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }


    //function setpixel4($img, $centerX, $centerY, $deltaX, $deltaY, $color)
    //{
    //    imagesetpixel($img, $centerX + $deltaX, $centerY + $deltaY, $color);
    //    imagesetpixel($img, $centerX - $deltaX, $centerY + $deltaY, $color);
    //    imagesetpixel($img, $centerX + $deltaX, $centerY - $deltaY, $color);
    //    imagesetpixel($img, $centerX - $deltaX, $centerY - $deltaY, $color);
    //}

    bool WuIsArcClear(Arc arc, float turnRadius, BoxCollider boxCollider)
    {
        if (MyMath.IsNearZero(arc.length))
            return true;

        float halfWidth = boxCollider.gameObject.transform.localScale.x * boxCollider.size.x / 2.0f;
        float halfLength = boxCollider.gameObject.transform.localScale.z * boxCollider.size.z / 2.0f;

        float radiusSqr = turnRadius * turnRadius;
        const float kAlphaCutoff = 0.1f;

        //$radiusX2 = $radiusX * $radiusX;
        //$radiusY2 = $radiusY * $radiusY;

        var quarter = Mathf.RoundToInt(radiusSqr / Mathf.Sqrt(radiusSqr + radiusSqr));

        for (int x = 0; x <= quarter; ++x)
        {
            float y = turnRadius * Mathf.Sqrt(1 - (x * x) / radiusSqr);
            float err = y - Mathf.Floor(y);

            //float transparency = Mathf.RoundToInt(err * )
            //  $error = $y - floor($y);
            //  $transparency = round($error * $maxTransparency);
            //  $alpha = $color | ($transparency << 24);
            //  $alpha2 = $color | (($maxTransparency - $transparency) << 24);
            //            setpixel4($img, $centerX, $centerY, $x, floor($y),   $alpha);
            //            setpixel4($img, $centerX, $centerY, $x, floor($y) + 1, $alpha2);

            //setpixel4($img, $centerX, $centerY, $x, floor($y),   $alpha);
            //setpixel4($img, $centerX, $centerY, $x, floor($y) + 1, $alpha2);


        }

        //static $maxTransparency = 0x7F; // 127
        //// upper and lower halves
        //$quarter = round($radiusX2 / sqrt($radiusX2 + $radiusY2));
        //        for ($x = 0; $x <= $quarter; $x++)
        //{
        //  $y = $radiusY* sqrt(1 -$x *$x /$radiusX2);
        //  $error = $y - floor($y);
        //  $transparency = round($error * $maxTransparency);
        //  $alpha = $color | ($transparency << 24);
        //  $alpha2 = $color | (($maxTransparency - $transparency) << 24);
        //            setpixel4($img, $centerX, $centerY, $x, floor($y),   $alpha);
        //            setpixel4($img, $centerX, $centerY, $x, floor($y) + 1, $alpha2);
        //        }
        //// right and left halves
        //$quarter = round($radiusY2 / sqrt($radiusX2 + $radiusY2));
        //        for ($y = 0; $y <= $quarter; $y++)
        //{
        //  $x = $radiusX* sqrt(1 -$y *$y /$radiusY2);
        //  $error = $x - floor($x);
        //  $transparency = round($error * $maxTransparency);
        //  $alpha = $color | ($transparency << 24);
        //  $alpha2 = $color | (($maxTransparency - $transparency) << 24);
        //    setpixel4($img, $centerX, $centerY, floor($x),   $y, $alpha);
        //    setpixel4($img, $centerX, $centerY, floor($x) + 1, $y, $alpha2);
        //}

        return false; 
    }

	bool TilesUnderLine(HashSet<int> tileIdxsVisited, Vector2 worldPos0, Vector2 worldPos1, float length, BoxCollider boxCollider)
	{
		//float distTraveled = 0.0f;
		float distInc = this.TileSize / kTileDiv;
		float tInc = distInc / length;
		
        // Get the perpendicular positions
        // TODO:  Note this doesn't take into account 3d position at all.
        Vector2 dirVec = new Vector2(worldPos1.x - worldPos0.x, worldPos1.y - worldPos0.y) / length;
		float halfBoxWidth = boxCollider.transform.localScale.x * boxCollider.size.x / 2.0f;
		Vector2 leftVec = new Vector2(-dirVec.y, dirVec.x) * halfBoxWidth;
        Vector2 rightVec = -leftVec;

		Vector2 forwardVec = dirVec * boxCollider.transform.localScale.z * boxCollider.size.z / 2.0f;
        // we bump and check the final worldPos1 + forwardVec
        worldPos1 += forwardVec;

        Vector2[]  positionsToCheck = new Vector2[3]; 
		for(float t = 0.0f; t < (1.0f + tInc); t += tInc) 
            // I added the (1.0 + tInc) to ensure we hit the end, Lerp already Clamps for us
		{
			positionsToCheck[0] = Vector2.Lerp(worldPos0, worldPos1, t); // center
            positionsToCheck[1] = positionsToCheck[0] + leftVec;	     // left
            positionsToCheck[2] = positionsToCheck[0] + rightVec;       //right 

            for (int i = 0; i < positionsToCheck.Length; ++i)
            {
                int idx = GetTileIndex(positionsToCheck[i]);
                if (idx != -1)
                {
                    tileIdxsVisited.Add(idx);
                }
            }
		} 
		
		return true;
	}

    bool IsLineClear(Vector2 worldPos0, Vector2 worldPos1, float length, BoxCollider boxCollider)
    {
        // TODO: This could be optimized by doing wu's line/ bresenham (with some tweaks, those seem more error prone
        //float distTraveled = 0.0f;
        float distInc = this.TileSize / kTileDiv;
        float tInc = distInc / length;

        // Get the perpendicular positions
        // TODO:  Note this doesn't take into account 3d position at all.
        Vector2 dirVec = new Vector2(worldPos1.x - worldPos0.x, worldPos1.y - worldPos0.y) / length;
        float halfBoxWidth = boxCollider.transform.localScale.x * boxCollider.size.x / 2.0f;
        Vector2 leftVec = new Vector2(-dirVec.y, dirVec.x) * halfBoxWidth;

        Vector2 forwardVec = dirVec * boxCollider.transform.localScale.z * boxCollider.size.z / 2.0f;
        // we bump and check the final worldPos1 + forwardVec
        worldPos1 += forwardVec;

        Vector2[] positionsToCheck = new Vector2[3];

        for (float t = 0.0f; t < (1.0f + tInc); t += tInc)
        // I added the (1.0 + tInc) to ensure we hit the end, Lerp already Clamps for us
        {
            positionsToCheck[0] = Vector2.Lerp(worldPos0, worldPos1, t); // center
            positionsToCheck[1] = positionsToCheck[0] + leftVec;	     // left
            positionsToCheck[2] = positionsToCheck[0] - leftVec;         //right 

            for (int i = 0; i < positionsToCheck.Length; ++i)
            {
                if (HighlightLineTraveled)
                {
                    int x = GetXTileIndex(positionsToCheck[i].x);
                    int y = GetYTileIndex(positionsToCheck[i].y);

                    var debugPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    debugPrimitive.transform.parent = this.CubeRoot.transform;
                    debugPrimitive.transform.position = new Vector3(x * TileSize + static_position.x, TileSize / 2, y * TileSize + static_position.z);
                    debugPrimitive.transform.localScale = new Vector3(this.TileSize, this.TileSize, this.TileSize) * .5f;
                    debugPrimitive.GetComponent<Renderer>().material = debugNode;
                    debugPrimitive.layer = LayerMask.NameToLayer("BlockLayer");
                }

                if (IsTileBlocked(positionsToCheck[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }

	float Fract(float x) { return x - Mathf.Floor(x);}
	float InvFract(float x) { return 1.0f - Fract(x);}
	bool Plot(Dictionary<int, TileSquare> tilesVisited, int x, int y, float b, bool stopOnBlock)
	{
		//if(Mathf.Approximately(0.0f, b))
		//	return true;
		//if( b < 0.01f)
		//	return true;
//		byte bf = (byte)(b * 255.0f);
//		if(bf < 100)
//			return true;
		
		if(b < 0.4)
			return true;
		
		if(IsValidTile(x,y))
		{
			int index = x + y * this.Width;
			if(!tilesVisited.ContainsKey(index))
			{
                //if(stopOnBlock && squares[index].isBlocked)
                //        return false;
				
				tilesVisited.Add(index, squares[index]);
				return true;
			}
		}
		
		return false;
	}
	public bool TilesUnderLineAlt(Dictionary<int, TileSquare> tilesVisited, Vector3 worldPos0, Vector3 worldPos1, bool stopOnBlock)
	{
		// convert to wu's line algorithm.
		Vector2 p1 = WorldToTile(worldPos0);
		Vector2 p2 = WorldToTile(worldPos1);
		
		float xd = p2.x - p1.x;
		float yd = p2.y - p1.y;
		
		bool isSteep = Mathf.Abs(yd) > Mathf.Abs(xd);
		
		float gradient;
		float xgap;
		
		int ix1, iy1, ix2, iy2;
		
		float b1, b2;
		float yf;
		bool ret;
		
		if(isSteep) 
		{
			//Swap x with y
			
			float tempf = p1.x;
			p1.x = p1.y;
			p1.y = tempf;
			
			tempf = p2.x;
			p2.x = p2.y;
			p2.y = tempf;
		}
		
		// horizontal line
		if(p1.x > p2.x)
		{
			//Swap
			Vector2 temp = p1;
			p1 = p2;
			p2 = temp;
			
			
		}
		
		xd = p2.x - p1.x; 
		yd = p2.y - p1.y; 
		
		gradient = yd / xd;
			
		// End Point 1
	    float xend = Mathf.Floor(p1.x + 0.5f);
		float yend = p1.y + gradient * (xend - p1.x);
		xgap = InvFract(p1.x + 0.5f);
			
		ix1  = (int)xend;
		iy1  = (int)yend;
		
		b1 = InvFract(yend) * xgap;
		b2 = Fract(yend) * xgap;
			
		if(isSteep)
		{
			ret = Plot(tilesVisited, iy1, ix1, b1, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
			
			ret = Plot(tilesVisited, iy1 + 1, ix1, b2, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
		}
		else
		{
			ret = Plot(tilesVisited, ix1, iy1, b1, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
			
			ret = Plot(tilesVisited, ix1, iy1 + 1, b2, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
		}
			
		yf = yend + gradient;
		
		// End Point 2 
		xend = Mathf.Floor(p2.x + 0.5f);
		yend = p2.y + gradient * (xend - p2.x);
		
		xgap = Fract(p2.x - 0.5f);
		
		ix2  = (int)xend;
		iy2  = (int)yend;
		
		b1 = InvFract(yend) * xgap;
		b2 = Fract(yend) * xgap;
		
		if(isSteep)
		{
			ret = Plot(tilesVisited, iy2, ix2, b1, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
			
			ret = Plot(tilesVisited, iy2 + 1, ix2, b2, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
		}
		else
		{
			ret = Plot(tilesVisited, ix2, iy2, b1, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
		
			ret = Plot(tilesVisited, ix2, iy2 + 1, b2, stopOnBlock);
			if(stopOnBlock && !ret)
				return false;
		}
		
		// Main Loop
		if(isSteep)
		{
			for(int x = ix1 + 1; x <= ix2; ++x) // this loop needs work
			{
				b1 = InvFract(yf);
		    	b2 = Fract(yf);
				
				Debug.Log("B1: " + b1);
				Debug.Log("B2: " + b2);
				 
				ret = Plot(tilesVisited, (int)yf, x, b1, stopOnBlock);
				if(stopOnBlock && !ret)
					return false;
		
				ret = Plot(tilesVisited, (int)yf+1,x , b2, stopOnBlock);
				if(stopOnBlock && !ret)
					return false;
				
				yf = yf + gradient;
			}
		}
		else
		{
			for(int x = ix1 +1; x <= ix2; ++x) // this loop needs work
			{
				b1 = InvFract(yf);
		    	b2 = Fract(yf);
				
				Debug.Log("B1: " + b1);
				Debug.Log("B2: " + b2);
				 
				ret = Plot(tilesVisited, x, (int)yf, b1, stopOnBlock);
				if(stopOnBlock && !ret)
					return false;
		
				ret = Plot(tilesVisited, x, (int)yf+1, b2, stopOnBlock);
				if(stopOnBlock && !ret)
					return false;
				
				yf = yf + gradient;
			}
		}
		
		return true;
	}

	public HashSet<int> TilesUnderDubin(DubinCSC csc, float turnRadius, BoxCollider boxCollider)
	{
		// Three parts
		//Dictionary<int, TileSquare> tilesVisited = new Dictionary<int, TileSquare>();  //index, tile
        HashSet<int> tileIdxsVisited = new HashSet<int>();
		
		//Get tiles under start arc
        TilesUnderArc(tileIdxsVisited, csc.startArc, turnRadius, boxCollider);
		
		//Get tiles under line
        TilesUnderLine(tileIdxsVisited, csc.line.start, csc.line.end, csc.sLength, boxCollider);
		//TilesUnderLine(tilesVisited, csc.line.start, csc.line.end, false);
		
		//Get Tiles under end arc
        TilesUnderArc(tileIdxsVisited, csc.endArc, turnRadius, boxCollider);

        return tileIdxsVisited;
	}
	
	public void HighlightTiles(List<TileSquare> squares)
	{
		foreach(TileSquare sq in debugSelectedSquares)
		{
			sq.debugPrimitive.GetComponent<Renderer>().material = debugNode;
		}
		
		foreach(TileSquare sq in squares)
		{
            
			sq.debugPrimitive.GetComponent<Renderer>().material = debugCollision;
		}
		
		debugSelectedSquares = squares;
		
		//return debugSelectedSquares;
	}
	
	public bool IsDubinClear(DubinCSC csc, float turnRadius, BoxCollider boxCollider)
	{
        return IsLineClear(csc.line.start, csc.line.end, csc.sLength, boxCollider) &&
               IsArcClear(csc.startArc, turnRadius, boxCollider) &&
               IsArcClear(csc.endArc, turnRadius, boxCollider);
	}
		 
	const float kHeuristicMultiplier = 100000.0f;
	
	delegate DubinCSC FindGoalDubin(DirectionalCircle orgLeft, 
							      DirectionalCircle orgRight, 
								  Vector2 ptStart,
								  Vector2 ptDest,
								  float turnRadius,
								  DirectionalCircle dstLeft,
								  DirectionalCircle dstRight);
	
	FindGoalDubin findGoalDubin;

    public struct FindPathArgs
    {
        public float TurnRadius;
        public Vector3 Fwd;
        public Vector3 Right;
        public Vector3 Pos;
        public Vector3 Dest;
        public Vector3 DestDir;
        public DirectionalCircle FinalDstLeft;  // These are 'private' args
        public DirectionalCircle FinalDstRight; // these are 'private args:  TODO: Make actually private
        public BoxCollider BoxCollider;
        public bool IsDirectionalPath;
    }

    int tileVisitPoolFreeIndex = 0;

    private void InitializeSearchQueue(
        BoxCollider boxCollider,
        float turnRadius, 
        Vector2 ptStart, 
        Vector2 ptDest, 
        DirectionalCircle orgLeft, 
        DirectionalCircle orgRight,
        int[] neighborOffsets,
        int offsetCount,
        DirectionalCircle finalDestLeft, 
        DirectionalCircle finalDestRight
        )
    {
        int startSqrIdx = GetTileIndex(ptStart); // I don't bother checking the start tile for validity

        // Walk through all the neighboring tiles
        for (int idx = 0; idx < offsetCount; idx++)
        {
            int neighborIdx = startSqrIdx + neighborOffsets[idx];

            if (!IsValidTile(neighborIdx) || isBlockedGrid[neighborIdx])
                continue; // skip invalid && blocked tiles

            Vector2 ptNeighbor = GetTilePointFromIndex(neighborIdx); // new Vector2(neighborSq.pos.x, neighborSq.pos.z);

            for (TileDirection d = TileDirection.N; d < TileDirection.NUM_DIRS; ++d)
            {
                // Should I do this check here?  It seems like it would be faster, but possibly  miss an important case?
                //Check the visited, if it has a longer heuristic value, replace/remove it.

                if (visitedTiles[neighborIdx * (int)d] == searchPass)
                {
                    //Technically, i should then look to see if the heuristic value is less than what it's currently rated for this visit.
                    continue; // skip tiles we've visited before
                }

                Vector2 offset = TileCoordinates.GetLeftCircleOffset(d);

                // Construct the destination circles for this tile direction
                DirectionalCircle destLeft = new DirectionalCircle(new Vector2(ptNeighbor.x + offset.x * turnRadius,
                                                                               ptNeighbor.y + offset.y * turnRadius),
                                                                               ptNeighbor, false);

                DirectionalCircle destRight = new DirectionalCircle(new Vector2(ptNeighbor.x - offset.x * turnRadius,
                                                                                ptNeighbor.y - offset.y * turnRadius),
                                                                                ptNeighbor, true);
                // Find all Dubins to this neighbor tile
                DubinCSC shortestDubin = DubinCSC.FindDubin(orgLeft, orgRight, ptStart, ptNeighbor, turnRadius, destLeft, destRight);

                // Take the shortest 'clear' dubin
                if (shortestDubin.IsValidDubin() && IsDubinClear(shortestDubin, turnRadius, boxCollider))
                {
                    TileVisit nVisit = tileVisitPool[tileVisitPoolFreeIndex++];
                    nVisit.InitializeTile(
                        newDirection: d,
                        aLeft: destLeft,
                        aRight: destRight,
                        aPrevTile: null,
                        aPath: shortestDubin,
                        aStartPoint: ptNeighbor
                        );

                    nVisit.goalDubin = findGoalDubin(nVisit.orgLeft, nVisit.orgRight, nVisit.ptStart, ptDest, turnRadius, finalDestLeft, finalDestRight);
                    if (nVisit.goalDubin.IsValidDubin())
                    {
                        // and Add it to the priority queue.
                        priorityQueue.Enqueue(nVisit, nVisit.CalculatePriority() * 10.0f + ((float)tileVisitPoolFreeIndex) / (float)tileVisitPool.Length);
                    }
                }
            }
        }
    }

    
    private bool DoFindPath(FindPathArgs a, out List<DubinCSC> path)
	{
        tileVisitPoolFreeIndex = 0;
		path = new List<DubinCSC>();

        int destIdx = GetTileIndex3(a.Dest);
        if (destIdx == -1 || isBlockedGrid[destIdx])
			return false;

		float turnRadius = a.TurnRadius; 
		
		
		// Create the directional circles
		Vector2 ptLeftCenter = new Vector2(a.Pos.x - a.Right.x * turnRadius, 
										   a.Pos.z - a.Right.z * turnRadius);

        Vector2 ptRightCenter = new Vector2(a.Pos.x + a.Right.x * turnRadius,
                                            a.Pos.z + a.Right.z * turnRadius);
		
		Vector2 ptDest   = new Vector2(a.Dest.x, a.Dest.z);
        Vector2 ptStart = new Vector2(a.Pos.x, a.Pos.z);

        var orgLeft =  new DirectionalCircle(ptLeftCenter, ptStart, false);
        var orgRight = new DirectionalCircle(ptRightCenter, ptStart, true);

        var goalDubin = findGoalDubin(orgLeft, orgRight, ptStart, ptDest, turnRadius, a.FinalDstLeft, a.FinalDstRight); 
        if (goalDubin.IsValidDubin() && IsDubinClear(goalDubin, turnRadius, a.BoxCollider))
	    {
            path.Add(goalDubin);

            // early out, we're done, we can just go straight to the goal
            return true;
        }
        
        // okay now we have to do some a* action
        priorityQueue.Clear();
        searchPass++; // todo if we flip, we should then reset and clear

        DubinRadiusTable radiusTable = this.dubinTable.GetTable(turnRadius);
		int [] neighborOffsets = bUse48Neighbors ? dir48 : dir24;
		int offsetCount = bUse48Neighbors ? dir48Length : dir24Length;

        // initialize the search queue wiht the inital values
        InitializeSearchQueue(a.BoxCollider, turnRadius, ptStart, ptDest, orgLeft, orgRight, neighborOffsets, offsetCount, a.FinalDstLeft, a.FinalDstRight);

		int magicFailCount = 0;

		while(priorityQueue.Count != 0)
		{
            TileVisit visit = priorityQueue.Dequeue();

			if(magicFailCount++ >= 1000)
			{
				return false;
			}

			// First, lets check if we can move directly to the goal from here
			// and end if we are
            if (visit.goalDubin.IsValidDubin() && IsDubinClear(visit.goalDubin, turnRadius, a.BoxCollider))
			{
                List<DubinCSC> dubinList = visit.GetAsDubinList();
                dubinList.Add(visit.goalDubin);
                path = dubinList;

                return true;
                //TODO do we want to check the alternate path??
            }
			
			// Okay we couldn't make it, lets check all the neighbors in all the cardinal directions
            int visitSqrIdx = GetTileIndex(visit.ptStart);

            // mark the tile so we don't try to double back on ourselves.
            visitedTiles[visitSqrIdx * (int)visit.direction] = searchPass;
			
			// Lets use the dubinTable
			GenerateNeighborsFromTable(radiusTable, 
										priorityQueue, 
					                    neighborOffsets, offsetCount,
					                    visitSqrIdx, visit,
                                        ptDest, a.FinalDstLeft, a.FinalDstRight,
                                        a.BoxCollider);
			
		}
		
		return false;
	}

    // Trying to optimize DoFindPath a little
    private void GenerateNeighborsFromTable(DubinRadiusTable radiusTable,
                                            PriorityQueueNode.HeapPriorityQueue<TileVisit> priorityQueue,
											int[] neighborOffsets, int offsetCount,  
											int visitSqrIdx, 
											TileVisit visit,
											Vector2 ptDest,
											DirectionalCircle finalDstLeft,
											DirectionalCircle finalDstRight,
                                            BoxCollider boxCollider)
	{
		float turnRadius = radiusTable.turnRadius;
        Vector2 ptVisit = GetTilePointFromIndex(visitSqrIdx);

		for(int idx = 0; idx < offsetCount; ++idx)
		{
			int neighborIdx = visitSqrIdx + neighborOffsets[idx];
			
			if(!IsValidTile(neighborIdx)) 
				continue; // skip invalid tiles
			
            if (isBlockedGrid[neighborIdx])
				continue; // skip blocked tiles

            Vector2 ptNeighbor = GetTilePointFromIndex(neighborIdx); // new Vector2(neighborSq.pos.x, neighborSq.pos.z);
			for(TileDirection d = TileDirection.N; d < TileDirection.NUM_DIRS; ++d)
			{
                if (visitedTiles[neighborIdx * (int)d] == searchPass)
                {
					continue; // skip tiles we've visited before
                    //TODO:  We could check cost here
				}
				
				DubinTableEntry entry = radiusTable.GetDubinEntry(visit.direction, idx, d, boxCollider);
				if(entry.OffsetDubin.IsValidDubin())
				{
					bool isClear = entry.offsets1D.Count > 0 ? true : false;
					for(int t = 0; t < entry.offsets1D.Count; ++t)
					{
                        int finalIndex = visitSqrIdx + entry.offsets1D[t];
                        if (!IsValidTile(finalIndex) || isBlockedGrid[finalIndex])
						{
							isClear = false;
							break;
						}
					}
					
					if(isClear)
					{
                        DirectionalCircle newLeftDC = entry.leftOffsetDC;
                        DirectionalCircle newRightDC = entry.rightOffsetDC;
                        newLeftDC.center += visit.ptStart;
                        newRightDC.center += visit.ptStart;

                        DubinCSC goalDubin = findGoalDubin(newLeftDC, newRightDC, ptNeighbor, ptDest, turnRadius, finalDstLeft, finalDstRight);
                        if (goalDubin.IsValidDubin())
                        {
                            TileVisit nVisit = tileVisitPool[tileVisitPoolFreeIndex++];
                            nVisit.InitializeTile(
                                    newDirection: d,
                                    aLeft: newLeftDC,
                                    aRight: newRightDC,
                                    aPrevTile: visit,
                                    aPath:  DubinCSC.Translate(entry.OffsetDubin, visit.ptStart),
                                    aStartPoint: ptNeighbor,
                                    aGoalDubin: goalDubin
                                    );

                            priorityQueue.Enqueue(nVisit, nVisit.CalculatePriority() * 10.0f + ((float)tileVisitPoolFreeIndex) / (float)tileVisitPool.Length);
                        }
					}
				}
			}
		}
	}
	
	
	// TODO:  This is a bit borked
    public bool FindPath(FindPathArgs args, out List<DubinCSC> path)
    {
        if (args.IsDirectionalPath)
        {
            this.findGoalDubin = DubinCSC.FindDubin;

            Vector2 finalOffset = new Vector2(-args.DestDir.y, args.DestDir.x);
            Vector2 ptDest = new Vector2(args.Dest.x, args.Dest.z);
            float turnRadius = args.TurnRadius;

            args.FinalDstLeft = new DirectionalCircle(new Vector2(ptDest.x + finalOffset.x * turnRadius,
                                                                                 ptDest.y + finalOffset.y * turnRadius), ptDest, false);

            args.FinalDstRight = new DirectionalCircle(new Vector2(ptDest.x - finalOffset.x * turnRadius,
                                                                                 ptDest.y - finalOffset.y * turnRadius), ptDest, true);
        }
        else
        {
            this.findGoalDubin = DubinCSC.FindDegenerateDubin;
        }

        return DoFindPath(args, out path);
    }

    public bool FindDirectionalPath(FindPathArgs args, out List<DubinCSC> path)
    {
        this.findGoalDubin = DubinCSC.FindDubin;

        Vector2 finalOffset = new Vector2(-args.DestDir.y, args.DestDir.x);
        Vector2 ptDest = new Vector2(args.Dest.x, args.Dest.z);
        float turnRadius = args.TurnRadius;

        args.FinalDstLeft = new DirectionalCircle(new Vector2(ptDest.x + finalOffset.x * turnRadius,
                                                                             ptDest.y + finalOffset.y * turnRadius), ptDest, false);

        args.FinalDstRight = new DirectionalCircle(new Vector2(ptDest.x - finalOffset.x * turnRadius,
                                                                             ptDest.y - finalOffset.y * turnRadius), ptDest, true);


        return DoFindPath(args, out path);
    }

    //public bool FindPath(TileMoveable tileMoveable, Vector3 dest, out List<DubinCSC> path)
    //{
    //    this.findGoalDubin = DubinCSC.FindDegenerateDubin;
    //    DirectionalCircle dummyCircle = new DirectionalCircle(Vector2.zero, false);
		
    //    Profiler.BeginSample("WTF");
    //    bool ret = DoFindPath( new  FindPathArgs() {
    //                            Fwd = tileMoveable.isReverse ?
    //                                  -tileMoveable.transform.forward : tileMoveable.transform.forward,

    //                            Right = tileMoveable.isReverse ?
    //                                    -tileMoveable.transform.right : tileMoveable.transform.right,

    //                            TurnRadius = tileMoveable.turnRadius,
    //                            Pos = tileMoveable.transform.position,
    //                            Dest = dest,
    //                            DestDir = Vector2.zero,
    //                            FinalDstLeft = dummyCircle,
    //                            FinalDstRight = dummyCircle,
    //                            BoxCollider = tileMoveable.boxCollider},
    //                            out path);
    //    Profiler.EndSample();
		
    //    return ret;
    //}
	
	
	// Finds the path with a given destination direction
    //public bool FindPath(TileMoveable tileMoveable, Vector3 dest, Vector2 destDir, out List<DubinCSC> path)
    //{
    //    this.findGoalDubin = DubinCSC.FindDubin;
		
    //    Vector2 finalOffset = new Vector2(-destDir.y, destDir.x);
    //    Vector2 ptDest   = new Vector2(dest.x, dest.z);
    //    float turnRadius = tileMoveable.turnRadius;
		
    //    DirectionalCircle finalDestLeft  = new DirectionalCircle(new Vector2(ptDest.x + finalOffset.x * turnRadius, 
    //                                                                         ptDest.y + finalOffset.y * turnRadius), ptDest, false);
		
    //    DirectionalCircle finalDestRight = new DirectionalCircle(new Vector2(ptDest.x - finalOffset.x * turnRadius, 
    //                                                                         ptDest.y - finalOffset.y * turnRadius), ptDest, true);

    //    Profiler.BeginSample("WTF");
    //    bool ret = DoFindPath(
    //        new FindPathArgs()
    //        {
    //            Fwd = tileMoveable.isReverse ?
    //                     -tileMoveable.transform.forward : tileMoveable.transform.forward,

    //            Right = tileMoveable.isReverse ?
    //                    -tileMoveable.transform.right : tileMoveable.transform.right,

    //            TurnRadius = tileMoveable.turnRadius,
    //            Pos = tileMoveable.transform.position,
    //            Dest = dest,
    //            DestDir = destDir,
    //            FinalDstLeft = finalDestLeft,
    //            FinalDstRight = finalDestRight,
    //            BoxCollider = tileMoveable.boxCollider
    //        },  
    //        out path);
    //    Profiler.EndSample();

    //    return ret;
    //}
}


//TODO:  Intellisense dies on this stupid thing for reasons unknown
//List<TileSquare> CircleTest(Vector2 center, float radius, float linewidth)
//    {
//        List<TileSquare> visited = new List<TileSquare>();
//        float feather = 1.0f;
		
//        int x, y;
//        int lx, rx, ly, ry;
//        int fact;
//        float ropf2, romf2, ripf2, rimf2;
//        float outrad, inrad;
		
//        outrad = radius + linewidth / 2.0f;
//        inrad  = radius - linewidth / 2.0f;
//        ropf2 = Mathf.Pow( (outrad + feather / 2.0f), 2);
//        romf2 = Mathf.Pow( (outrad - feather / 2.0f), 2);
//        ripf2 = Mathf.Pow( (inrad + feather / 2.0f), 2);
//        rimf2 = Mathf.Pow( (inrad - feather / 2.0f), 2);
		
//        //note to self, this system is 0 at the top...probably not correct
//        // determine bounds
//        lx = Mathf.Max(Mathf.FloorToInt(center.x - ropf2), 0);
//        rx = Mathf.Min(Mathf.CeilToInt (center.x + ropf2), this.Width - 1);
		
//        ly = Mathf.Max(Mathf.FloorToInt(center.y + ropf2), 0);
//        ry = Mathf.Min(Mathf.CeilToInt (center.y + ropf2), this.Height - 1);
		
//        if(feather > linewidth) feather = linewidth;
		
//        float[] sqX = new float[rx - lx + 1];  
//        for(x = lx; x <= rx; ++x)
//        {
//            sqX[x - lx] = (x - center.x) * (x - center.x);
//        }
