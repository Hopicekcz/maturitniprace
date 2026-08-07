using UnityEngine;

public class DestroyableMaterial : MonoBehaviour
{

    private GameObject thisItem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisItem = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BreakGlass()
    {
        Destroy(thisItem);
    }
}
