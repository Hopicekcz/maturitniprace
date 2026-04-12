using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

public class DoorAnimationScript : MonoBehaviour
{
    public bool _triggerEntered = false;
    [SerializeField]
    private Animator animator;

    public bool doorOpen;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;
    [SerializeField] private Collider Collider;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        CheckForPlayer();
    }

    private void CheckForPlayer(){
        if (_triggerEntered && Keyboard.current[Key.E].wasPressedThisFrame)
        {
            UseDoor();
        }
    }

    private void UseDoor(){
        if(doorOpen && (animator.GetCurrentAnimatorStateInfo (0).IsName ("door-opened"))){
            CloseDoor();
        } else if (!doorOpen && (animator.GetCurrentAnimatorStateInfo (0).IsName ("door-closed"))){
            OpenDoor();
        }
    }

    public void OpenDoor(){
        doorOpen = true;
        audioSource.Stop();
        animator.SetTrigger("Open");
        audioSource.clip = doorOpenClip;
        while(!audioSource.isPlaying){
            audioSource.Play();
        }
    }

    public void CloseDoor(){
        doorOpen = false;
        audioSource.Stop();
        animator.SetTrigger("Close");
        audioSource.clip = doorCloseClip;
        while(!audioSource.isPlaying){
            audioSource.Play();
        }
    }


    
}