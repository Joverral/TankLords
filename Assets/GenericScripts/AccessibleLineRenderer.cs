using UnityEngine;
using System.Collections;
using System;

// The GameObject requires a LineRender component (because they sealed the class like assholes)
[RequireComponent(typeof(LineRenderer))]
public class AccessibleLineRenderer : MonoBehaviour {

    Vector3[] vertices = new Vector3[0]{};
    public LineRenderer UnderlyingLineRenderer;

    public void SetPosition(int index, Vector3 position)
    {
        vertices[index] = position;
        UnderlyingLineRenderer.SetPosition(index, position);
    }

    public void SetVertexCount(int count)
    {
        Array.Resize(ref vertices, count);
        UnderlyingLineRenderer.SetVertexCount(count);
    }

    public Vector3 GetPosition(int index)
    {
        return vertices[index];
    }

    public int Length { get { return vertices.Length;  } }
}
