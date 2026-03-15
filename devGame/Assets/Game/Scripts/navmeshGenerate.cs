using UnityEngine;
using Unity.AI.Navigation;
public class navmeshGenerate : MonoBehaviour
{
    public NavMeshSurface surface;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        surface = this.GetComponent<NavMeshSurface>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
