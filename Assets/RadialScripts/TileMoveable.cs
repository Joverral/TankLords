using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


static public class MyMath
{
    static public  bool HackApproximately(float a, float b)
    {

        return (a + float.Epsilon >= b) && (a - float.Epsilon <= b);
    }

    static public bool IsNearZero(float a)
    {
        return (a <= float.Epsilon) && (a >= -float.Epsilon);
    }
}

public struct Arc
{
    public Arc(DirectionalCircle in_circle, Vector2 ptStart, Vector2 ptEnd, float radius)
    : this(in_circle, Mathf.Atan2(ptStart.y - in_circle.center.y, ptStart.x - in_circle.center.x), ptEnd, radius)
    { }

    public Arc(DirectionalCircle in_circle, Vector2 ptEnd, float radius) 
    : this(in_circle, in_circle.startTheta, ptEnd, radius)
    {
    }

    public Arc(DirectionalCircle in_circle, float strtTheta, Vector2 ptEnd, float radius) 
	{
		this.circle = in_circle;
        this.circle.startTheta = strtTheta;
		deltaAngle = this.circle.Angle(strtTheta, ptEnd);
		this.endTheta = strtTheta + deltaAngle;
		this.length = Mathf.Abs(deltaAngle * radius);
	}
	
    // TODO:  Technically we know the turnradius, it's already baked into the length...
    public void GetPositionAndHeadingFromDistance(float relativeCurrentDistance, float turnRadius, out Vector3 pos, out Vector3 dir)
    {
        GetPositionAndHeadingFromTValue(relativeCurrentDistance / turnRadius, turnRadius, out pos, out dir);
    }

    public void GetPositionAndHeadingFromTValue(float t, float turnRadius, out Vector3 pos, out Vector3 dir)
    {
        // we're on the start arc
        float rotDir = circle.clockwise == true ? -1.0f : 1.0f;
        float angle = startTheta + rotDir * t;

        pos = new Vector3(circle.center.x + turnRadius * Mathf.Cos(angle),
                          0.0f,
                          circle.center.y + turnRadius * Mathf.Sin(angle));

        dir = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
        if (circle.clockwise)
            dir = -dir;
    }

	public  DirectionalCircle circle;
	public  float startTheta {  get { return circle.startTheta; } set { circle.startTheta = value; } }
	public  float endTheta;
	public  float length;

    public float deltaAngle;


    public void UpdateStartTheta(float relativeStartDistance, float turnRadius)
    {
        float rotDir = circle.clockwise == true ? -1.0f : 1.0f;
        float angle = startTheta + rotDir * (relativeStartDistance / turnRadius);

        startTheta = angle;
        this.length = Mathf.Abs(deltaAngle * turnRadius);
    }

    public override string ToString()
    {
        float rotDir = circle.clockwise == true ? -1.0f : 1.0f;
        float angle = startTheta + rotDir * this.endTheta;

        return string.Format("startTheta: {0} endTheta: {1} Angle: {2} Delta: {3}", startTheta, endTheta, angle, deltaAngle);
    }
}

public struct DubinCSC
{
	public Arc startArc;
	public Line2D line;
	public Arc endArc;
	
	public float sLength; // length of the straight Line2D 
    // public float totalLength;//TODO: can I get away with not computing the total length until I've found my Dubins, keep it all sqrs?

    public float totalLength;

    public float TotalLengthSquared { get; set; }

	public static DubinCSC TakeShortest(DubinCSC du1, DubinCSC du2)
	{
        return du1.totalLength > du2.totalLength ? du2 : du1;
	}
	
	static public DubinCSC RSRCourse(
								 DirectionalCircle orgRight, 
								 DirectionalCircle dstRight,
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius)
	{
		
		// Compute tangent line
		float x1 = orgRight.center.x;
		float y1 = orgRight.center.y;
		
		float x2 = dstRight.center.x;
		float y2 = dstRight.center.y;
	
		Vector2 v = dstRight.center - orgRight.center;
		
		float dSqr = v.sqrMagnitude;
		
		if(dSqr < turnRadius)
		{
			// Circles overlap
			return NullDubin;
		}
		
		float d = Mathf.Sqrt(dSqr);
		v /= d; // note to self, i believe this is overkill, and just there for circles of different radius, normal doesn't need to be computed.
		// and i could get away with a simple x1,y1 + -vy,vx   
		// And I might be able to get away with checking each individial arc/line/arc distance separately
		
		// Sign1 = 1, Sign2 = 1
		float nx = /*v.x * c*/ - v.y;
		float ny = /*v.y * c*/ + v.x;
		
		nx *= turnRadius;
		ny *= turnRadius;
		
		DubinCSC course = new DubinCSC();
		
		course.line = new Line2D() {  start = new Vector2(x1 + nx, y1 + ny), 
								      end   = new Vector2(x2 + nx, y2 + ny)};
		
		course.sLength = d;
		
		course.startArc = new Arc(orgRight, orgRight.startTheta, course.line.start, turnRadius);
		course.endArc   = new Arc(dstRight, course.line.end, ptDest, turnRadius);
		course.totalLength = course.sLength + course.startArc.length + course.endArc.length;
		
		return course;
	}
	
	static public DubinCSC LSLCourse( 
								 DirectionalCircle orgLeft, 
								 DirectionalCircle dstLeft,
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius)
	{
		float x1 = orgLeft.center.x;
		float y1 = orgLeft.center.y;
		
		float x2 = dstLeft.center.x;
		float y2 = dstLeft.center.y;
	
		Vector2 v = dstLeft.center - orgLeft.center;
		
		float dSqr = v.sqrMagnitude;
		
		if(dSqr < turnRadius)
		{
			// Circles overlap
			return NullDubin;
		}
		
		float d = Mathf.Sqrt(dSqr);
		v /= d;
		
		float nx = /*v.x * c*/ - v.y;
		float ny = /*v.y * c*/ + v.x;
		
		// Sign1 = 1, Sign2 = -1
		nx = -nx * turnRadius;
		ny = -ny * turnRadius;
		
		DubinCSC course = new DubinCSC();
		
		course.line =  (new Line2D() { start = new Vector2(x1 + nx, y1 + ny), 
			                           end =   new Vector2(x2 + nx, y2 + ny)});
		
		course.sLength = d;
		
		course.startArc = new Arc(orgLeft, orgLeft.startTheta, course.line.start, turnRadius);
		course.endArc   = new Arc(dstLeft, course.line.end, ptDest, turnRadius);
		
		course.totalLength = course.sLength + course.startArc.length + course.endArc.length;
		
		return course;
	}
	
	static public DubinCSC RSLCourse( 
								 DirectionalCircle orgRight, 
								 DirectionalCircle dstLeft,
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius)
	{
		float x1 = orgRight.center.x;
		float y1 = orgRight.center.y;
		
		float x2 = dstLeft.center.x;
		float y2 = dstLeft.center.y;
	
		Vector2 v = dstLeft.center - orgRight.center;
		
		float dSqr = v.sqrMagnitude;
		
		if(dSqr < turnRadius)
		{
			// Circles overlap
			return NullDubin;
		}
		
		float d = Mathf.Sqrt(dSqr);
		v /= d;
		
		// For Sign1 = -1
		float c = (turnRadius + turnRadius) / d;
		if(c*c > 1.0f)
			return NullDubin;
		
		float h = Mathf.Sqrt(1.0f - c*c);
		
		// Sign1 = -1, Sign2 = 1
		float nx = (v.x * c - h * v.y) * turnRadius;
		float ny = (v.y * c + h * v.x) * turnRadius;
		
		DubinCSC course = new DubinCSC();
		
		course.line =( new Line2D() { start = new Vector2(x1 + nx, y1 + ny), 
									  end   = new Vector2(x2 - nx, y2 - ny)});
		
		
		course.startArc = new Arc(orgRight, orgRight.startTheta, course.line.start, turnRadius);
		course.endArc   = new Arc(dstLeft, course.line.end, ptDest, turnRadius);
		course.sLength = d;
		course.totalLength = course.sLength + course.startArc.length + course.endArc.length;
		
		return course;
	}
	
	static public DubinCSC LSRCourse( 
								 DirectionalCircle orgLeft, 
								 DirectionalCircle dstRight,
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius)
	{
		float x1 = orgLeft.center.x;
		float y1 = orgLeft.center.y;
		
		float x2 = dstRight.center.x;
		float y2 = dstRight.center.y;
	
		Vector2 v = dstRight.center - orgLeft.center;
		
		float dSqr = v.sqrMagnitude;
		
		if(dSqr < turnRadius)
		{
			// Circles overlap
			return NullDubin;
		}
		
		float d = Mathf.Sqrt(dSqr);
		v /= d;
		
		// For Sign1 = -1
		float c = (turnRadius + turnRadius) / d;
		
		if(c*c > 1.0f)
			return NullDubin;
		
		float h = Mathf.Sqrt(1.0f - c*c);
		
		// Sign1 = -1, Sign2 = -1
		float nx = (v.x * c + h * v.y) * turnRadius;
		float ny = (v.y * c - h * v.x) * turnRadius;
		
		DubinCSC course = new DubinCSC();
		
		course.line = ( new Line2D() { start = new Vector2(x1 + nx, y1 + ny),
			                            end =  new Vector2(x2 - nx, y2 - ny)});
		course.sLength = d;
		
		course.startArc = new Arc(orgLeft, orgLeft.startTheta, course.line.start, turnRadius);
		course.endArc = new Arc(dstRight, course.line.end, ptDest, turnRadius);
		
		course.totalLength = course.sLength + course.startArc.length + course.endArc.length;
		
		return course;
	}
	
	// Generates four dubins
	static public DubinCSC FindDubins(DirectionalCircle orgLeft, 
							      DirectionalCircle orgRight, 
								  DirectionalCircle dstLeft,
								  DirectionalCircle dstRight,
								  Vector2 ptStart,
								  Vector2 ptDest,
								  float turnRadius,
								  DubinCSC[] dubins)
	{

        dubins = new DubinCSC[4];
        // RSR
        ///////
        DubinCSC shortestDubins = dubins[0] = DubinCSC.RSRCourse(orgRight, dstRight, ptStart, ptDest, turnRadius);
		
		// LSL 
		////////
		dubins[1] = DubinCSC.LSLCourse(orgLeft, dstLeft, ptStart, ptDest, turnRadius);
		shortestDubins = DubinCSC.TakeShortest(shortestDubins, dubins[1]);
		
		// RSL
		////////
		dubins[2] = DubinCSC.RSLCourse(orgRight, dstLeft, ptStart, ptDest, turnRadius);
		shortestDubins = DubinCSC.TakeShortest(shortestDubins, dubins[2]);
		
		// LSR
		///////
		dubins[3] = DubinCSC.LSRCourse(orgLeft, dstRight, ptStart, ptDest, turnRadius);
		shortestDubins = DubinCSC.TakeShortest(shortestDubins, dubins[3]);
		
		return shortestDubins;
	}
	
	static public DubinCSC FindDubin(DirectionalCircle orgLeft, 
							      DirectionalCircle orgRight, 
								  Vector2 ptStart,
								  Vector2 ptDest,
								  float turnRadius,
								  DirectionalCircle dstLeft,
								  DirectionalCircle dstRight)
	{
		// RSR
		///////
		DubinCSC shortestDubins = DubinCSC.RSRCourse(orgRight, dstRight, ptStart, ptDest, turnRadius);
		
		// LSL 
		////////
		shortestDubins = DubinCSC.TakeShortest(shortestDubins, DubinCSC.LSLCourse(orgLeft, dstLeft, ptStart, ptDest, turnRadius));
		
		// RSL
		////////
		shortestDubins = DubinCSC.TakeShortest(shortestDubins, DubinCSC.RSLCourse(orgRight, dstLeft, ptStart, ptDest, turnRadius));
		
		// LSR
		///////
		return DubinCSC.TakeShortest(shortestDubins, DubinCSC.LSRCourse(orgLeft, dstRight, ptStart, ptDest, turnRadius));
	}
	
	// note:  This form doesn't care about ending direction, just tries to get to the point
	// Generates two dubins
	static public DubinCSC FindDegenerateDubins(
								 DirectionalCircle orgLeft, 
								 DirectionalCircle orgRight, 
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius,
								 DubinCSC[] dubins)
	{
		Vector2 vLeft  = orgLeft.center  - ptDest;  // lines from center of circle to destination 
		Vector2 vRight = orgRight.center - ptDest; 
		
		DubinCSC dubinLS = NullDubin;  
		DubinCSC dubinRS = NullDubin;  
		
		float rSq = turnRadius * turnRadius;  // this could probably be pulled out to a member or argument
		
		if(rSq < vLeft.sqrMagnitude)
		{
			float a = Mathf.Asin(turnRadius / vLeft.magnitude); // vLeft.Mag = h
			float b = Mathf.Atan2(vLeft.y, vLeft.x);
			
			float t = b + a;
			Vector2 Q = new Vector2(orgLeft.center.x + turnRadius * -Mathf.Sin(t),
				                    orgLeft.center.y + turnRadius * Mathf.Cos(t));
			dubinLS = new DubinCSC();
			dubinLS.startArc = new Arc(orgLeft, orgLeft.startTheta, Q, turnRadius);
			dubinLS.line.start = Q;
			dubinLS.line.end   = ptDest;
			dubinLS.sLength = (dubinLS.line.end - dubinLS.line.start).magnitude;
			
			dubinLS.endArc.startTheta = 0.0f;
			dubinLS.endArc.endTheta = 0.0f;
			dubinLS.endArc.length = 0.0f;
			
			dubinLS.totalLength = dubinLS.startArc.length + dubinLS.sLength;
		}
		
		if(rSq < vRight.sqrMagnitude)
		{
			float a = Mathf.Asin(turnRadius / vRight.magnitude);
			float b = Mathf.Atan2(vRight.y, vRight.x);
			float t = b - a;
			Vector2 Q = new Vector2(orgRight.center.x + turnRadius * Mathf.Sin(t),
				                    orgRight.center.y + turnRadius * -Mathf.Cos(t));
			dubinRS = new DubinCSC();
			dubinRS.startArc = new Arc(orgRight, orgRight.startTheta, Q, turnRadius);
			dubinRS.line.start = Q;
			dubinRS.line.end   = ptDest;
			dubinRS.sLength = (dubinRS.line.end - dubinRS.line.start).magnitude;
			
			dubinRS.endArc.startTheta = 0.0f;
			dubinRS.endArc.endTheta = 0.0f;
			dubinRS.endArc.length = 0.0f;
			
			dubinRS.totalLength = dubinRS.startArc.length + dubinRS.sLength;
		}
		
		
		DubinCSC shortDubin = DubinCSC.TakeShortest(dubinLS, dubinRS);
		
		dubins[0] = dubinLS;
		dubins[1] = dubinRS;
		
		
		return shortDubin;
	}
	
	static public DubinCSC FindDegenerateDubins(
								 DirectionalCircle orgLeft, 
								 DirectionalCircle orgRight, 
								 Vector2 ptStart,
								 Vector2 ptDest,
								 float turnRadius)
	{
		Vector2 vLeft  = orgLeft.center  - ptDest;
		Vector2 vRight = orgRight.center - ptDest;
		
		DubinCSC dubinLS = new DubinCSC();   
		DubinCSC dubinRS = new DubinCSC();   
		
		float rSq = turnRadius * turnRadius;
		
		if(rSq < vLeft.sqrMagnitude)
		{
			float a = Mathf.Asin(turnRadius / vLeft.magnitude);
			float b = Mathf.Atan2(vLeft.y, vLeft.x);
			
			float t = b + a;
			Vector2 Q = new Vector2(orgLeft.center.x + turnRadius * -Mathf.Sin(t),
				                    orgLeft.center.y + turnRadius * Mathf.Cos(t));
			

            // TODO:  I think I'm doing one too many Atan2s here....
			dubinLS.startArc = new Arc(orgLeft, orgLeft.startTheta, Q, turnRadius);
			dubinLS.line.start = Q;
			dubinLS.line.end   = ptDest;
			dubinLS.sLength = (dubinLS.line.end - dubinLS.line.start).magnitude;
			
			dubinLS.endArc.startTheta = 0.0f;
			dubinLS.endArc.endTheta = 0.0f;
			dubinLS.endArc.length = 0.0f;
			
			dubinLS.totalLength = dubinLS.startArc.length + dubinLS.sLength;
		}
        else
        {
            dubinLS.totalLength = NullDubin.totalLength;
        }
		
		if(rSq < vRight.sqrMagnitude)
		{
			float a = Mathf.Asin(turnRadius / vRight.magnitude);
			float b = Mathf.Atan2(vRight.y, vRight.x);
			float t = b - a;
			Vector2 Q = new Vector2(orgRight.center.x + turnRadius * Mathf.Sin(t),
				                    orgRight.center.y + turnRadius * -Mathf.Cos(t));
			
			dubinRS.startArc = new Arc(orgRight, orgRight.startTheta, Q, turnRadius);
			dubinRS.line.start = Q;
			dubinRS.line.end   = ptDest;
			dubinRS.sLength = (dubinRS.line.end - dubinRS.line.start).magnitude;
			
			dubinRS.endArc.startTheta = 0.0f;
			dubinRS.endArc.endTheta = 0.0f;
			dubinRS.endArc.length = 0.0f;
			
			dubinRS.totalLength = dubinRS.startArc.length + dubinRS.sLength;
		}
        else
        {
            dubinRS.totalLength = NullDubin.totalLength;
        }

        return DubinCSC.TakeShortest(dubinLS, dubinRS);
	}
	
	// This just cheats and calls the above function, used because it has the same function signature as 
	// the directional one
	static public DubinCSC FindDegenerateDubin(DirectionalCircle orgLeft, 
							      DirectionalCircle orgRight, 
								  Vector2 ptStart,
								  Vector2 ptDest,
								  float turnRadius,
								  DirectionalCircle dstLeft,
								  DirectionalCircle dstRight)
	{
		return FindDegenerateDubins(orgLeft, orgRight, ptStart, ptDest, turnRadius);
	}
	
	
	
	static public DubinCSC Translate(DubinCSC dubin, Vector2 transVec)
	{
        // Lengths remain the same, just the positions change
        DubinCSC transDubin = new DubinCSC()
        {
            startArc = dubin.startArc,
            line = dubin.line,
            endArc = dubin.endArc,
            sLength = dubin.sLength,
            totalLength = dubin.totalLength
        };

		transDubin.line.start += transVec;
		transDubin.line.end   += transVec;
		transDubin.startArc.circle.center += transVec;
		transDubin.endArc.circle.center   += transVec;
		
		return transDubin;
	}


    static private Vector3 Vec2To3(Vector2 vec2)
    {
        return new Vector3(vec2.x, 1.0f, vec2.y);
    }

    Vector2 Traverse(float timeElapsed, float totalTime, float turnRadius)
    {
        //TODO I should move this to another structure and save all these one time calculations
        // find how much time should be spent on each segment
        //TODO I should probably return the facing/direction as well.....

        float startArcTime = (this.startArc.length / this.totalLength) * totalTime;
        float lineTime     = (this.sLength / this.totalLength) * totalTime;
        float endArcTime =   (this.startArc.length / this.totalLength) * totalTime;

        if (timeElapsed < startArcTime)
        {
            float angle = startArc.startTheta + (timeElapsed / startArcTime) * (startArc.endTheta - startArc.startTheta);
            return new Vector2(startArc.circle.center.x + turnRadius * Mathf.Cos(angle), startArc.circle.center.y + turnRadius * Mathf.Sin(angle));
        }
        else if (timeElapsed < (startArcTime + lineTime))
        {
            return Vector2.Lerp(this.line.start, this.line.end, (timeElapsed - startArcTime / lineTime));
        }
        else // must be on the endArc
        {
            float angle = endArc.startTheta + ((timeElapsed - startArcTime - lineTime) / endArcTime) * (endArc.endTheta - endArc.startTheta);
            return new Vector2(endArc.circle.center.x + turnRadius * Mathf.Cos(angle), endArc.circle.center.y + turnRadius * Mathf.Sin(angle));
        }
    }

    // Given a distance that is less than or equal to the length of the dubin curve, and the turn radius
    //TODO:  I should probably move to re-including the turn radius, space saving be damned
    public void GetPositionAndHeadingFromDistance(float relativeCurrentDistance, float turnRadius, out Vector3 pos, out Vector3 dir)
    {
        if (startArc.length > relativeCurrentDistance)
        {
            startArc.GetPositionAndHeadingFromDistance(relativeCurrentDistance, turnRadius, out pos, out dir);
        }
        else if ((startArc.length + sLength) > relativeCurrentDistance || endArc.length == 0.0f)
        {
            // we're on the line
            float currentDistanceOnLine = relativeCurrentDistance - startArc.length;
            float t = currentDistanceOnLine / sLength;
                
            Vector2 lerp = Vector2.Lerp(line.start, line.end, t);
            pos = new Vector3(lerp.x, 0.0f, lerp.y);
                
            Vector2 dir2d = line.end - line.start;
            dir2d.Normalize();
            dir = new Vector3(dir2d.x, 0, dir2d.y);
        }
        else // we're on the end arc
        {
            float currentDistanceOnEndArc = relativeCurrentDistance - (startArc.length + sLength);
            endArc.GetPositionAndHeadingFromDistance(currentDistanceOnEndArc, turnRadius, out pos, out dir);
        }
    }

    public void Clip(float relativeStartDistance, float turnRadius)
    {
        // remove the start arc length from total (it will either get re-added or stay removed)
        this.totalLength -= startArc.length;

        // find out where we are on the new starting dubin, and adjust starting point
        if (startArc.length > relativeStartDistance)
        {
          
            startArc.UpdateStartTheta(relativeStartDistance, turnRadius);
            this.totalLength += startArc.length;
        }
        else // we're past the start arc
        {
            // clip the start arc out
            startArc.length = 0.0f;

            // remove the line contribution to total length (it will either get re-added or stay removed)
            this.totalLength -= sLength;
            if ((startArc.length + sLength) > relativeStartDistance)
            {
                // we're on the line, update position and length
                float currentDistanceOnLine = relativeStartDistance - startArc.length;
                float t = currentDistanceOnLine / sLength;
                line.start = Vector2.Lerp(line.start, line.end, t);
                sLength = Vector2.Distance(line.start, line.end);
                this.totalLength += sLength;
            }
            else // we're past the line
            {
                sLength = 0.0f;
                if(totalLength < relativeStartDistance)
                {
                    this.totalLength = 0.0f; // oddball case?
                    Debug.LogError("I didn't think it would get here");
                }
                else
                {
                    this.totalLength -= endArc.length;
                    endArc.UpdateStartTheta(relativeStartDistance, turnRadius);
                    this.totalLength += endArc.length;
                }
            }
                  
        }
    }

    public float GetHValue()
    {
      //float h =  (this.startArc.length + this.endArc.length) + this.sLength;
      //float rotDir = startArc.circle.clockwise == true ? -1.0f : 1.0f;
      //float angle = startArc.startTheta + rotDir * startArc.endTheta;
      //if (angle > Mathf.PI || angle < -Mathf.PI)
      //    h *= 1000;

      //angle = endArc.startTheta + rotDir * endArc.endTheta;
      //if (angle > Mathf.PI || angle < -Mathf.PI)
      //    h *= 1000;

      //return h;

        return this.totalLength;

    }

    public static DubinCSC NullDubin = new DubinCSC() { totalLength = float.MaxValue };

    public bool IsValidDubin() { return this.totalLength != float.MaxValue;  }
}

public struct Line2D
{
	public Vector2 start;
	public Vector2 end;
}

public struct DirectionalCircle
{
	public float startTheta;
	public Vector2 center;
	//public float radius;  // Lets not bother, stored once on tilemoveable
	public bool clockwise;

	private float RestrictAngle(float theta)
	{
        if (MyMath.HackApproximately(theta, 0.0f))
		{
			theta = 0.0f;
		}
 		else if (theta < 0.0f && (!this.clockwise))
		{
			theta += 2.0f * Mathf.PI;
			if (theta < Mathf.Epsilon  ||  theta > (2.0f * Mathf.PI - Mathf.Epsilon))
	        	return 0.0f;
		}
 		else if (theta > 0.0f && (this.clockwise))
		{
			theta -= 2.0f * Mathf.PI;
			if (theta > Mathf.Epsilon  ||  theta < -(2.0f * Mathf.PI - Mathf.Epsilon))
	        	return 0.0f;
		}
		
		return theta;
	}
	
	public DirectionalCircle(Vector2 ptCenter, TileDirection tileDir, bool bClockwise)
	{
		this.center = ptCenter;
		this.clockwise = bClockwise;
		
		if(this.clockwise)
		{
			//		startTheta = this.clockwise ? Mathf.PI : 0.0f;
			startTheta = -Mathf.PI;
			startTheta -= (Mathf.PI/4.0f) * (int)tileDir;
			
//			if(startTheta > Mathf.PI * 2.0f)
//			{
//				startTheta -= Mathf.PI * 2.0f;
//			}
			
//			if(startTheta > Mathf.PI * 2.0f)
//			{
//				startTheta -= Mathf.PI * 2.0f;
//			}
		}
		else
		{
			startTheta = 0;
			startTheta += (Mathf.PI/4.0f) *(int)tileDir;
			
			
//			if(startTheta < -Mathf.PI * 2.0f)
//			{
//				startTheta += Mathf.PI * 2.0f;
//			}
			
			
		}
		
		if(startTheta > Mathf.PI)
		{
			startTheta -= Mathf.PI*2.0f;
		}
		
		if(startTheta < -Mathf.PI)
			startTheta += Mathf.PI*2.0f;

        if (MyMath.IsNearZero(startTheta))
		{
			startTheta = 0.0f;
		}
//		if(startTheta > Mathf.PI)
//			startTheta -= Mathf.PI;
		
		//startTheta = RestrictAngle(startTheta);
		
//		startTheta = this.clockwise ? Mathf.PI : 0.0f;
//		startTheta += (Mathf.PI/4.0f) * (int)tileDir;
//		
//		if(startTheta > Mathf.PI * 2.0f)
//		{
//			startTheta -= Mathf.PI * 2.0f;
//		}
	}
		
	public DirectionalCircle(Vector2 ptCenter, bool bClockwise)
	{
		this.center = ptCenter;
		this.clockwise = bClockwise;
		startTheta = 0.0f;
	}
	
	public DirectionalCircle(Vector2 ptCenter, Vector2 ptStart, bool bClockwise)
	{
		this.center = ptCenter;
		this.clockwise = bClockwise;

        this.startTheta = Mathf.Atan2(ptStart.y - this.center.y,
                                      ptStart.x - this.center.x); 

        //Vector2 angleVec = (ptStart - ptCenter).normalized;
        //float dot = Vector2.Dot(angleVec, Vector2.right);

        //if (dot != 0)
        //{
        //    float deltaAngle = Mathf.Acos(dot);
        //    float rightDot = Vector2.Dot(angleVec, Vector2.down);
        //    if (rightDot > 0)
        //    {
        //        startTheta = deltaAngle;
        //    }
        //    else if (rightDot < 0)
        //    {
        //        startTheta = Mathf.PI * 2.0f - deltaAngle;
        //    }
        //    else
        //    {
        //        startTheta = deltaAngle;
        //    }
        //}
        //else
        //{
        //    startTheta = 0;
        //}

        //float trueTheta = Mathf.Atan2(ptStart.y - this.center.y,
        //                              ptStart.x - this.center.x);

        //if (trueTheta != startTheta)
        //{
        //    startTheta = trueTheta;
        //}
    }

    public float Angle(Vector2 lPt, Vector2 rPt)
	{
		Vector2 lVec = lPt - this.center;
		return this.Angle(Mathf.Atan2(lVec.y,lVec.x), rPt);
	}
	
	public float Angle(float startTheta, Vector2 rPt)
	{
		Vector2 rVec = rPt - this.center;

	 	float theta = Mathf.Atan2(rVec.y, rVec.x) - startTheta;
		
		
		return RestrictAngle(theta);
	}
	
}


public class MoveablePath
{
    public float turnRadius;
    public List<DubinCSC> path;
    public bool isReverse;
    public bool isDirectional; //is the end direction specific, or just try to get to the point, not care about direction
    public float moveRateCost;
    public Speed speed;
    // TODO:
    // totalDistance?
    // 

    public Vector3 GetEndDirectionVec3()
    {
        Vector3 dir;
        Vector3 pos;
        GetEndDirAndPos(out dir, out pos);
        
        return dir; // Note IsReverse is checked via GetEndDirAndPos
    }

    public void GetEndDirAndPos(out Vector3 dir, out Vector3 pos)
    {
        DubinCSC lastDubin = path[path.Count - 1];

        if (isDirectional)
        {
            lastDubin.endArc.GetPositionAndHeadingFromDistance(
                lastDubin.endArc.length,
                turnRadius,
                out pos,
                out dir);
        }
        else
        {
            Vector2 normDir2D = (lastDubin.line.end - lastDubin.line.start).normalized;
            dir = new Vector3(normDir2D.x, 0.0f, normDir2D.y);
            pos = new Vector3(lastDubin.line.end.x, 0.0f, lastDubin.line.end.y);
        }

        if(isReverse)
        {
            dir = -dir;
        }
    }

    public void RenderToLine(AccessibleLineRenderer lineRenderer, float lineWidth)
    {
        var args = new LineRenderArgs()
        {
            LineWidth = lineWidth,
            StartDistance = 0.0f,
            EndDistance = CalculateTotalDistance()
        };

        DrawDubin(lineRenderer, args);
    }

    public void RenderToLine(AccessibleLineRenderer lineRenderer, LineRenderArgs args)
    {
        DrawDubin(lineRenderer, args);
    }

    public struct HeadingPositionPair
    {
        public HeadingPositionPair(Vector3 pos, Vector3 dir)
        {
            Pos = pos;
            Dir = dir;
        }
        public readonly Vector3 Pos;
        public readonly Vector3 Dir;
    }

    public void Clip(float startDistance)
    {
        // find the new starting dubin:
        float pathDistance = 0.0f;
        DubinCSC newStartDubin = DubinCSC.NullDubin;
        int startIdx = -1;

        // TODO make this into a function, though it's an awkward one
        // Figure out which Dubin in the list we should start on
        for (int i = 0; i < path.Count; ++i)
        {
            pathDistance += path[i].totalLength;
            if (startDistance < pathDistance)
            {
                newStartDubin = path[i];
                startIdx = i;
                pathDistance -= path[i].totalLength;
                break; //Found our dubin
            }
        }

        if (startIdx == -1 || MyMath.IsNearZero(pathDistance))
        {
            this.path.Clear();
        }
        else
        {
            float relativeDistanceStart = startDistance - pathDistance;
            newStartDubin.Clip(relativeDistanceStart, turnRadius);
             
            // remove all previous dubins
            this.path.RemoveRange(0, startIdx);
        }
    }

    public List<HeadingPositionPair> AsPointsList(LineRenderArgs args, float distInc)
    {
        Vector3 nextPos = Vector3.zero;
        Vector3 nextDir;
        int dubinIndex = path.Count - 1;

        float totalLength = this.CalculateTotalDistance();

        float pathDistance = 0.0f;
        DubinCSC currentDubin = path[path.Count - 1];
        // Figure out which Dubin in the list we should start on
        for (int i = 0; i < path.Count; ++i)
        {
            pathDistance += path[i].totalLength;
            if (args.StartDistance <= pathDistance)
            {
                currentDubin = path[i];
                dubinIndex = i;
                pathDistance -= path[i].totalLength;
                break; //Found our dubin
            }
        }

       // const int numSegmentsPerDubin = 100;
       // int totalNumSegments = numSegmentsPerDubin * (path.Count - dubinIndex);
        //float distInc = Mathf.Ceil(currentDubin.totalLength / numSegmentsPerDubin);
        int numPairs = (int)(totalLength / distInc);
        List<HeadingPositionPair> pathLines = new List<HeadingPositionPair>(numPairs);

        for (float distance = args.StartDistance; distance < args.EndDistance; )
        {
            float relativeDistanceStart = distance - pathDistance;
            float relativeDistanceEnd = Mathf.Min(currentDubin.totalLength, args.EndDistance - pathDistance);
            //float distInc = Mathf.Ceil(currentDubin.totalLength / numSegmentsPerDubin);

            for (float relativeDistance = relativeDistanceStart;
                   relativeDistance <= relativeDistanceEnd;
                   relativeDistance += distInc)
            {
                currentDubin.GetPositionAndHeadingFromDistance(relativeDistance, turnRadius, out nextPos, out nextDir);
                pathLines.Add(new HeadingPositionPair(nextPos, nextDir));
            }

            // ensure that the last spot is included
            currentDubin.GetPositionAndHeadingFromDistance(relativeDistanceEnd, turnRadius, out nextPos, out nextDir);
            pathLines.Add(new HeadingPositionPair(nextPos, nextDir));

            distance += currentDubin.totalLength;
            pathDistance += currentDubin.totalLength;

            dubinIndex++;
            if (dubinIndex >= path.Count)
            {
                break;
            }

            currentDubin = path[dubinIndex];
        }

        return pathLines;
    }

    public float CalculateTotalDistance()
    {
        // TODO:  Store this someplace, move to constructor
        float totalDistanceOfDubinPath = 0.0f;
        for (int i = 0; i < path.Count; ++i)
        {
            totalDistanceOfDubinPath += path[i].totalLength;
        }
        return totalDistanceOfDubinPath;
    }

    // Note this function will return the last dubin if the distance > the totalLength
    public int GetDubinIdxForDistance(float distance)
    {
        float dubinDistance = 0.0f;
        // Figure out which Dubin in the list we should be on.
        for (int i = 0; i < path.Count; ++i)
        {
            dubinDistance += path[i].totalLength;
            if (distance < dubinDistance)
            {
                return i; //found our dubin
            }
        }

        return -1;
    }

    public struct LineRenderArgs
    {
        public float LineWidth;
        public float StartDistance;
        public float EndDistance;
    }
    public void DrawDubin(AccessibleLineRenderer lineRenderer, LineRenderArgs args)
    {
        if (path == null || path.Count == 0)
        {
            //TEMP
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.UnderlyingLineRenderer.SetWidth(args.LineWidth, args.LineWidth);

        //TEMP
        if (lineRenderer.enabled == false)
        {
            lineRenderer.enabled = true;
        }

        Vector3 nextPos = Vector3.zero;
        Vector3 nextDir;
        int dubinIndex = path.Count - 1;

        float totalLength = this.CalculateTotalDistance();

        float pathDistance = 0.0f;
        DubinCSC currentDubin = path[path.Count - 1];
        // Figure out which Dubin in the list we should start on
        for (int i = 0; i < path.Count; ++i)
        {
            pathDistance += path[i].totalLength;
            if (args.StartDistance <= pathDistance)
            {
                currentDubin = path[i];
                dubinIndex = i;
                pathDistance -= path[i].totalLength;
                break; //Found our dubin
            }
        }

        const int numSegmentsPerDubin = 100;
        int totalNumSegments = numSegmentsPerDubin * (path.Count - dubinIndex);
        lineRenderer.SetVertexCount(totalNumSegments); //hackery
        int idx = totalNumSegments - 1;

        for (float distance = args.StartDistance; distance < args.EndDistance; )
        {
            float relativeDistanceStart = distance - pathDistance;
            float relativeDistanceEnd = Mathf.Min(currentDubin.totalLength, args.EndDistance - pathDistance);
            float distInc = Mathf.Ceil(currentDubin.totalLength / numSegmentsPerDubin);

             for (float relativeDistance = relativeDistanceStart;
                    relativeDistance <= relativeDistanceEnd;
                    relativeDistance += distInc)
             {
                 currentDubin.GetPositionAndHeadingFromDistance(relativeDistance, turnRadius, out nextPos, out nextDir);
                 nextPos.y += 1.0f;
                 lineRenderer.SetPosition(idx--, nextPos);
             }

             // ensure that the last spot is included
             currentDubin.GetPositionAndHeadingFromDistance(relativeDistanceEnd, turnRadius, out nextPos, out nextDir);
             nextPos.y += 1.0f;
             lineRenderer.SetPosition(idx, nextPos);

             distance += currentDubin.totalLength;
             pathDistance += currentDubin.totalLength;

             dubinIndex++;
             if (dubinIndex >= path.Count)
             {
                 break;
             }

             currentDubin = path[dubinIndex];
        }

        int badMath = 0;
        //for (int i = idx; i < totalNumSegments; i++)
        for (int i = idx; i >= 0; --i)
        {
            badMath++;
            lineRenderer.SetPosition(i, nextPos);
        }

        

  //      Debug.Log("BadMath: " + badMath);

    }
}

public class TileMoveable : MonoBehaviour {


    // TODO:  What is this doing here?
	Component CopyComponent(Component original, GameObject destination)
	{
		System.Type type = original.GetType();
		Component copy = destination.AddComponent(type);
		// Copied fields can be restricted with BindingFlags
		System.Reflection.FieldInfo[] fields = type.GetFields(); 
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(original));
		}
		return copy;
	}


    public float ModelScale
    {
        get { return moveableModel.transform.localScale.x; }
        set
        {
			if(value != ModelScale)
			{
				this.moveableModel.transform.localScale = new Vector3(value, value, value);
				Debug.Log("Old ID: " + boxCollider.GetInstanceID().ToString());
				BoxCollider newCollider = CopyComponent(boxCollider, this.moveableModel) as BoxCollider;
				DestroyImmediate(boxCollider);

				this.boxCollider = newCollider; // this is to get a new ID, 
				Debug.Log("New ID: " + this.boxCollider.GetInstanceID().ToString());
			}
        }
    }

    public float ModelWidth
    {
        get
        {
            return this.boxCollider.size.x * this.boxCollider.transform.localScale.x;
        }
    }

    public float MaxMoveDistance = 100;
    public float CurrentMoveDistance = 100;

    public BoxCollider boxCollider; 
	public GameObject moveableModel;
	public LineRenderer lineRenderer;
    public Transform ghostTransform;

	// Constrain a radian value to 0<=value<=2*PI
	public static float CapRadian(float r)
	{
    	while (r >= 2.0f * Mathf.PI)
        	r -= 2.0f * Mathf.PI;
    	while (r < 0.0f)
        	r += 2.0f * Mathf.PI;
    	if (r < Mathf.Epsilon  ||  r > (2.0f * Mathf.PI - Mathf.Epsilon))
        	return 0.0f;
    	return r;
	}
	
	public int tileX, tileY;
	public int activatedX, activatedY;

    MoveablePath currentPath;
    List<MoveablePath.HeadingPositionPair> pointList;
    public MoveablePath CurrentPath
    {
        get { return currentPath; }
        set
        {
            currentPath = value;
            
            gameEventSystem.RaiseEvent(GameEventType.PathChanged, this.gameObject, currentPath);
        }
    }

    GameEventSystem gameEventSystem;

    public bool isMoving;
    public float moveRate = 10.0f;
    float currentDistanceAlongDubinPath;
    float finalDistanceAlongDubinPath;

    float runningDubinDistanceTotal = 0.0f;
    int currentDubinIdx = 0;


   
     bool hasMoved = false;
    

  

	// Use this for initialization
	void Start () {
		//Create Hit check table for the turn radius(es?) here.

		this.activatedX = this.tileX;
		this.activatedY = this.tileY;
		
		TileGrid.Instance().AddToGrid(this);
        CurrentMoveDistance = MaxMoveDistance;

        
        var go = GameObject.FindGameObjectWithTag("GameEventSystem");
        gameEventSystem = go.GetComponent<GameEventSystem>();
       // gameEventSystem.Subscribe(this.gameObject, GameEventType.SpeedChanged, DiscreteSpeedChanged);
	}



	// Update is called once per frame
	void Update () 
	{
        if(isMoving)
        {
            // How long has it been since we last updated our position
            float distanceIncrement = this.moveRate * Time.deltaTime;
            // find out how far we should move
            // convert to T value 0...1
            this.currentDistanceAlongDubinPath += distanceIncrement;
            if (currentDistanceAlongDubinPath >= finalDistanceAlongDubinPath)
            {
                distanceIncrement = currentDistanceAlongDubinPath - finalDistanceAlongDubinPath;
                currentDistanceAlongDubinPath = finalDistanceAlongDubinPath;
                isMoving = false;
            }
            else
            {
                this.CurrentMoveDistance -= distanceIncrement;
            }

			this.CurrentMoveDistance = Mathf.Max(0.0f, CurrentMoveDistance);

            DubinCSC currentDubin = CurrentPath.path[this.currentDubinIdx];
            float relativeDistance = currentDistanceAlongDubinPath - runningDubinDistanceTotal;

            if (relativeDistance > currentDubin.totalLength)
            {
                this.runningDubinDistanceTotal += currentDubin.totalLength;
                relativeDistance -= currentDubin.totalLength;

                currentDubinIdx++;
                currentDubin = this.currentPath.path[currentDubinIdx];
            }

            Vector3 nextPos;
            Vector3 nextDir;
            
            currentDubin.GetPositionAndHeadingFromDistance(relativeDistance, this.currentPath.turnRadius, out nextPos, out nextDir);

            // move to that spot.
            this.transform.position = nextPos;
            this.transform.forward =  this.CurrentPath.isReverse ? -nextDir : nextDir;

            gameEventSystem.RaiseEvent(GameEventType.ObjectPropertyChanged, this.gameObject, this.gameObject);

            // TODO:  
            // Do something with tile grid?
            // Do we care for in motion stuff, or just the end position?

            if(!isMoving)
            {
                ////TODO: Do we really want to clear the entire, or just from the last point?
                //// in which case, we'd need to clip the dubin, basicaly....or repath
                //this.CurrentPath.path.Clear();
                this.CurrentPath.Clip(currentDistanceAlongDubinPath);
				gameEventSystem.RaiseEvent(GameEventType.PathChanged, this.gameObject, CurrentPath);
                gameEventSystem.RaiseEvent(GameEventType.MoveEnded, this.gameObject, CurrentPath);
            }
        }
	}

    public void BeginMove()
    {
        if(!isMoving && this.CurrentPath != null)
        {
             finalDistanceAlongDubinPath = Mathf.Min(CurrentPath.CalculateTotalDistance(), this.CurrentMoveDistance);
            if(finalDistanceAlongDubinPath > 0.0f)
            {
                isMoving = true;
                currentDistanceAlongDubinPath = 0.0f;

                this.runningDubinDistanceTotal = 0.0f; 
                currentDubinIdx = 0;

                gameEventSystem.RaiseEvent(GameEventType.MoveStarted, this.gameObject, this.CurrentPath);
            }
        }
    }

    public void OnEndTurn()
    {
        if(isMoving)
        {
            // I don't know, teleport to the end?, or should i disallow ending the turn during a move?
        }

        // TODO:  Status updates, etc
        this.CurrentMoveDistance = this.MaxMoveDistance;
        gameEventSystem.RaiseEvent(GameEventType.ObjectPropertyChanged, this.gameObject, this.gameObject);
    }


   
}
