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
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
        btnsfx.Stop();
        Planepartfx.Stop();
        coinSfx.Stop();
    }

   
   public void btnSFX()
   {
       btnsfx.Play();
   }

   public void PlanepartSFX()
   {
       Planepartfx.Play();
   }

   public void MarkerSFX()
   {
       markerSFX.Play();
   }
   public void CoinSFX()
   {
        coinSfx.Play();
   }
}
