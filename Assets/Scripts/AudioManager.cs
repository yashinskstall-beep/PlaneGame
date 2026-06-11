using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public AudioSource btnsfx;
    public AudioSource Planepartfx;
    public AudioSource audioSource;
    public AudioSource markerSFX;
    public AudioSource coinSfx;
    public AudioSource boostSFX;

    void Awake()
    {
        SettingsManager.LoadSavedSettings();
        SettingsManager.ApplySavedAudioState();
        SettingsManager.ApplySavedVibrationState();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (SettingsManager.IsAudioEnabled && audioSource != null)
            audioSource.Play();
        btnsfx.Stop();
        Planepartfx.Stop();
        coinSfx.Stop();
        boostSFX.Stop();
    }

   
   private bool CanPlaySfx()
   {
       return SettingsManager.IsAudioEnabled;
   }

   public void btnSFX()
   {
       if (CanPlaySfx() && btnsfx != null)
           btnsfx.Play();
   }

   public void PlanepartSFX()
   {
       if (CanPlaySfx() && Planepartfx != null)
           Planepartfx.Play();
   }

   public void MarkerSFX()
   {
       if (CanPlaySfx() && markerSFX != null)
           markerSFX.Play();
   }

   public void CoinSFX()
   {
       if (CanPlaySfx() && coinSfx != null)
           coinSfx.Play();
   }

   public void BoostSFX()
   {
       if (CanPlaySfx() && boostSFX != null)
           boostSFX.Play();
   }
}
