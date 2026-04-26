using UnityEngine;

public class DoorScriptTrigger : MonoBehaviour
{

    [SerializeField] private DoorAnimationScript DoorAnimationScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("PlayerController"))
        {
            Debug.Log("Detected!");
            DoorAnimationScript._triggerEntered = true;
        }
        if ((other.CompareTag("Character") || other.CompareTag("CharacterListener")) && (DoorAnimationScript.doorOpen == false))
        {
            DoorAnimationScript.OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("PlayerController"))
        {
            DoorAnimationScript._triggerEntered = false;
        }
    }
}
