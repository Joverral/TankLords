using UnityEngine;
using System.Collections;

public class MaterialSwapper : MonoBehaviour {

    [SerializeField]
    Material glowMaterial;

    Material[] orgMaterials;
    Material[] glowMaterials;
    MeshRenderer meshRenderer;
	// Use this for initialization
	void Start () {

        meshRenderer = this.GetComponent<MeshRenderer>();
        orgMaterials = new Material[meshRenderer.materials.Length];
        glowMaterials = new Material[meshRenderer.materials.Length];
        for(int i = 0; i < meshRenderer.materials.Length; ++i)
        {
            orgMaterials[i] = meshRenderer.materials[i];
            glowMaterials[i] = glowMaterial;
        }
       // orgMaterial = meshRenderer.materials[index];
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void SwapToGlowMaterial(int index)
    {
        Debug.Log("Swapping to Glow Material");
        
            meshRenderer.materials = glowMaterials;
        
    }

    public void SwapBack(int index)
    {
        Debug.Log("Swapping Back");
        
            meshRenderer.materials = orgMaterials;
        
    }
    
}
