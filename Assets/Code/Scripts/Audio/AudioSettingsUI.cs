
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // Recuperar los valores guardados
        masterSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("MasterVolume", 1f)
        );

        musicSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("MusicVolume", 1f)
        );

        sfxSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("SFXVolume", 1f)
        );

        // Conectar los sliders con el AudioManager
        masterSlider.onValueChanged.AddListener(
            AudioManager.Instance.SetMasterVolume
        );

        musicSlider.onValueChanged.AddListener(
            AudioManager.Instance.SetMusicVolume
        );

        sfxSlider.onValueChanged.AddListener(
            AudioManager.Instance.SetSFXVolume
        );
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance == null) return;

        masterSlider.onValueChanged.RemoveListener(
            AudioManager.Instance.SetMasterVolume
        );

        musicSlider.onValueChanged.RemoveListener(
            AudioManager.Instance.SetMusicVolume
        );

        sfxSlider.onValueChanged.RemoveListener(
            AudioManager.Instance.SetSFXVolume
        );
    }
}
