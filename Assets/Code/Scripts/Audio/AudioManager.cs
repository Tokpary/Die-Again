
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

    [Header("Reward Audio")]
    [SerializeField] private AudioClip _coinRewardSelectionSfx;

    [Header("Menu Audio")]
    [SerializeField] private AudioClip _curtainSfx;
    [SerializeField] private AudioClip _settingsGearsSfx;

    [Header("Shop Audio")]
    [SerializeField] private AudioClip _shopPostSfx;

    [Header("Container Audio")]
    [SerializeField] private AudioClip _chestSfx;
    [SerializeField] private AudioClip _drawerSfx;

    [Header("Gameplay Audio")]
    [SerializeField] private AudioClip _winningScoreSfx;

    [Header("Roulette Audio")]
    [SerializeField] private AudioClip _rouletteTickSfx;
    [SerializeField, Range(0f, 1f)]
    private float _rouletteTickVolume = 0.4f;

    [SerializeField] private AudioClip _normalRouletteResultSfx;
    [SerializeField] private AudioClip _bossRouletteResultSfx;

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

    public void PlayCoinRewardSelectionSFX()
    {
        PlaySFX(_coinRewardSelectionSfx);
    }

    public void PlayCurtainSFX()
    {
        PlaySFX(_curtainSfx);
    }

    public void PlaySettingsGearsSFX()
    {
        PlaySFX(_settingsGearsSfx);
    }

    public void PlayShopPostSFX()
    {
        PlaySFX(_shopPostSfx);
    }

    public void PlayChestSFX()
    {
        PlaySFX(_chestSfx);
    }

    public void PlayDrawerSFX()
    {
        PlaySFX(_drawerSfx);
    }

    public void PlayWinningScoreSFX()
    {
        PlaySFX(_winningScoreSfx);
    }

    // Sonido al cruzar cada separacion de la ruleta.
    public void PlayRouletteTickSFX()
    {
        if (_rouletteTickSfx == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(
            _rouletteTickSfx,
            _rouletteTickVolume
        );
    }

    public void PlayNormalRouletteResultSFX()
    {
        PlaySFX(_normalRouletteResultSfx);
    }

    public void PlayBossRouletteResultSFX()
    {
        PlaySFX(_bossRouletteResultSfx);
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
