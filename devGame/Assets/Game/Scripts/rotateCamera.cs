using UnityEngine;

public class rotateCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float mouse = Input.GetAxis("Mouse Y");
        transform.Rotate(new Vector3(1,0,0));
    }
}
