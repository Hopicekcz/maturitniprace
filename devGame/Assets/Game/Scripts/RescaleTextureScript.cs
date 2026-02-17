
using UnityEngine;

[ExecuteInEditMode]
public class RescaleTextureScript : MonoBehaviour
{
    // Reference to the original material with the texture
     Material originalMaterial;

    void Start()
    {
        // Ensure we have a material
        originalMaterial = GetComponent<Renderer>().material;

        // Create a new material instance for this object
        Material materialInstance = new Material(originalMaterial);

        // Apply the new material to the object
        GetComponent<Renderer>().material = materialInstance;

        // Get the initial scale of the object
        Vector3 initialScale = transform.localScale;

        // Set the texture tiling based on the initial scale
        SetTextureTiling(materialInstance, initialScale);
    }

    void Update()
    {
        // Adjust texture tiling based on the current scale
        SetTextureTiling(GetComponent<Renderer>().material, transform.localScale);
    }

    void SetTextureTiling(Material material, Vector3 scale)
    {
        // Calculate tiling based on the scale
        Vector2 tiling = new Vector2(scale.x, scale.z);

        // Apply tiling to the material
        material.mainTextureScale = tiling;
    }
}
