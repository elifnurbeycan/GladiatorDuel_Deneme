using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Audio")]
    public AudioSource menuMusicSource; // Menüdeki "MenuMusic" objesini buraya bağlayacağız

    private void Start()
    {
        // 1. Kayıtlı ses ayarlarını yükle (Yoksa 0.5 yap)
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        float sfxVol   = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // 2. Sliderları ayarla
        musicSlider.value = musicVol;
        sfxSlider.value   = sfxVol;

        // 3. Menü müziğinin sesini ayarla
        if (menuMusicSource != null)
        {
            menuMusicSource.volume = musicVol;
        }
    }

    public void OnNewGameClicked()
    {
        SceneManager.LoadScene(1); // 1 numaralı sahneye (SampleScene) git
    }

    public void OnMusicSliderChanged(float value)
    {
        // Değeri kaydet
        PlayerPrefs.SetFloat("MusicVolume", value);
        
        // Anlık olarak menü müziğini değiştir (Duyarak test etmek için)
        if (menuMusicSource != null)
        {
            menuMusicSource.volume = value;
        }
    }

    public void OnSFXSliderChanged(float value)
    {
        // Sadece kaydet (Efekt sesi menüde çalmadığı için burada duyulmaz)
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
}