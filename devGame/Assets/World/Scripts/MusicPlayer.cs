using System.Collections; 
using System.Collections.Generic; 
using UnityEngine;

public class MusicPlayer : MonoBehaviour 
{
    public AudioClip[] musicClips; 
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
            lastNumber = randomNumber;
            audioSource.clip = musicClips[randomNumber];
            audioSource.Play();
            }
        }

    }
}
