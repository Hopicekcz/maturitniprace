using System.Collections; 
using System.Collections.Generic; 
using UnityEngine;

public class MusicPlayer : MonoBehaviour 
{
    public AudioClip[] musicClips; 

    public float volume = 1f; 
    private AudioSource audioSource;
    private int randomNumber;
    private int lastNumber;

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); 
    }

    private void GetRandomNumber() {
        randomNumber = Random.Range(0, musicClips.Length);
    }

    void Update() 
    {
        if(audioSource.isPlaying == false){
            GetRandomNumber();
            if(!(randomNumber == lastNumber)){
            audioSource.clip = musicClips[randomNumber];
            audioSource.Play();
            }
        }

    }
}
