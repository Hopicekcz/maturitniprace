using System.Collections; // importuje kolekce pro praci s listy a poli
using System.Collections.Generic; // importuje genericke kolekce jako list nebo dictionary
using UnityEngine; // importuje unity engine knihovnu pro pristup ke komponentam a hernim objektum

[RequireComponent(typeof(AudioSource))] // zajisti ze objekt ma komponentu audiosource, jinak ji unity automaticky prida
public class MusicPlayer : MonoBehaviour // definuje tridu musicplayer, ktera dedi z monobehaviour
{
    [Header("Music Settings")] // prida nadpis v inspektoru unity pro lepsi prehlednost
    [Tooltip("Drag your MP3 or AudioClip here.")] // zobrazi napovedu v inspektoru kdyz najedes mysi
    public AudioClip musicClip; // promenna pro ulozeni hudebniho souboru, ktery bude prehravan

    [Range(0f, 1f)] // vytvori posuvnik v inspektoru pro nastaveni hlasitosti mezi 0 a 1
    [Tooltip("Adjust the volume of the music.")] // napoveda pro uzivatele v inspektoru
    public float volume = 1f; // promenna pro nastaveni hlasitosti hudby s vychozi hodnotou 1

    private AudioSource audioSource; // soukroma promenna pro ulozeni odkazu na komponentu audiosource

    void Start() // metoda, ktera se spusti jednou pri startu hry
    {
        audioSource = GetComponent<AudioSource>(); // ziska komponentu audiosource z objektu na kterem je skript pripojen

        if (musicClip != null) // zkontroluje jestli je do inspektoru prirazena hudebni nahravka
        {
            audioSource.clip = musicClip; // nastavi hudebni nahravku do audiosource
            audioSource.loop = true; // zapne opakovani hudby po jejim dohrani
            audioSource.playOnAwake = false; // vypne automaticke spusteni hudby pri startu hry
            audioSource.volume = volume; // nastavi hlasitost audiosource podle slideru
            audioSource.Play(); // spusti prehravani hudby
        }
        else // kdyz neni hudba nastavena
        {
            Debug.LogWarning("No music clip assigned to MusicPlayer!"); // vypise varovani do konzole unity
        }
    }

    void Update() // metoda, ktera se spousti kazdy snimek
    {
        audioSource.volume = volume; // neustale aktualizuje hlasitost podle slideru v inspektoru
    }
}
