
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadSettings();
    }

    // Convierte un volumen lineal (0-1) a decibelios.
    private float LinearToDecibels(float volume)
    {
        return volume <= 0.0001f
            ? -80f
            : Mathf.Log10(volume) * 20f;
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(
            MASTER_PARAM,
            LinearToDecibels(volume)
        );

        PlayerPrefs.SetFloat(MASTER_KEY, volume);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat(
            MUSIC_PARAM,
            LinearToDecibels(volume)
        );

        PlayerPrefs.SetFloat(MUSIC_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(
            SFX_PARAM,
            LinearToDecibels(volume)
        );

        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    // Reproducir música
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // Reproducir efectos
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // Recuperar configuración guardada
    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            PlayerPrefs.Save();
    }
}
