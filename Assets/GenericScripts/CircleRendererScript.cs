using UnityEngine;
using System.Collections;

public class CircleRendererScript : MonoBehaviour {

    public LineRenderer circleRenderer;
    public CircleType circleType;

    public enum CircleType
    {
        Full,
        Half,
        Quarter
    }
	// Use this for initialization
	void Start () {
        MakeCircle(circleRenderer, 1.0f);
	}

    private void MakeCircle(LineRenderer lineRenderer, float radius)
    {
        //float fidelity = Mathf.PI  / 180.0f;

        float endAngle;
        switch (circleType)
        {
            case CircleType.Full:
                endAngle = 2.0f * Mathf.PI;
                break;
            case CircleType.Half:
                endAngle = Mathf.PI;
                break;
            case CircleType.Quarter:
                endAngle = Mathf.PI / 2.0f;
                break;
            default:
                throw new UnassignedReferenceException();
        }

        float fidelity = Mathf.Deg2Rad * 12.0f;
        int count = 0;
        int finalCount = (int)(endAngle * Mathf.Rad2Deg / 12.0f);
        lineRenderer.SetVertexCount(finalCount + 2);

        for (float angle = 0; angle <= endAngle; angle += fidelity)
        {
            lineRenderer.SetPosition(count++, new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius)); // + this.transform.position
        }
        lineRenderer.SetPosition(count, new Vector3(Mathf.Cos(endAngle) * radius, 0.0f, Mathf.Sin(endAngle) * radius)); // + this.transform.position

    }
}
