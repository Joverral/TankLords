using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelController : MonoBehaviour {

    [SerializeField]
    Transform GhostTransform;
    [SerializeField]
    Transform RenderTransform;

    [SerializeField]
    Shader TransparentShader;

    [SerializeField]
    Shader OriginalShader;

    [SerializeField]
    MeshRenderer[] GameModelMeshes;

    List<MeshRenderer> internalMeshes = new List<MeshRenderer>();

    void Start()
    {
        var damageableArray = this.GetComponentsInChildren<DamageableComponent>();

        for(int i = 0; i < damageableArray.Length; ++i)
        {
            if(damageableArray[i].InternalModel != null)
            {
                var localMesh = damageableArray[i].InternalModel.GetComponent<MeshRenderer>();

                if (localMesh != null)
                {
                    internalMeshes.Add(localMesh);
                }
                else
                {
                    internalMeshes.AddRange(
                        damageableArray[i].InternalModel.GetComponentsInChildren<MeshRenderer>()
                        );
                }

            }
        }
        
    }

    private void SetRenderModelEnabled(bool value)
    {
        for (int i = 0; i < GameModelMeshes.Length; ++i)
        {
            GameModelMeshes[i].enabled = value;
        }
    }

    public void HideRenderModel()
    {
        SetRenderModelEnabled(false);
    }
    public void ShowRenderModel()
    {
        SetRenderModelEnabled(true);
    }

    void SetInternalRender(bool value)
    {
        for(int i = 0; i< internalMeshes.Count; ++i)
        {
            internalMeshes[i].enabled = value;
        }
    }

    public void ShowInternals()
    {
        SetInternalRender(true);
    }

    public void HideInternals()
    {
        SetInternalRender(false);
    }

    public void ShowGhost()
    {
        GhostTransform.gameObject.SetActive(true);
    }
    public void HideGhost()
    {
        GhostTransform.gameObject.SetActive(false);
    }


    public void SetGhostTransform(Transform otherTransform)
    {
        GhostTransform.parent = this.transform;
        GhostTransform.position = otherTransform.position;
        GhostTransform.rotation = otherTransform.rotation;
    }
}
