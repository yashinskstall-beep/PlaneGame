using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PlaneController planeController;    
    public GameObject boostBtn;
    public bool boostBtnActive = false;
    public GameObject boosters;
    // Start is called before the first frame update
    // Update is called once per frame
    void Update()
    {
        if (planeController.isControlling == true && boosters.activeSelf == true )
        {
           boostBtn.SetActive(true);

        }else{
            boostBtn.SetActive(false);          
        }
    }
        

   
}
