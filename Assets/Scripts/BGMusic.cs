using UnityEngine;

public class BGMusic : MonoBehaviour
{
    private AudioSource _audioSource;
    private bool isPlaying = false;
    private void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
        _audioSource = GetComponent<AudioSource>();
    }

    public void PlayMusic()
    {
        if (_audioSource.isPlaying && isPlaying) return;
        _audioSource.Play();
        isPlaying = true;
    }

    public void StopMusic()
    {
        _audioSource.Stop();
        isPlaying = false;
    }
}
