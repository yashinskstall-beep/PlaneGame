using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script allows toggling plane parts on/off for testing damage effects.
/// Attach this to a UI manager or the main camera.
/// </summary>
public class PlanePartToggler : MonoBehaviour
{
    [Header("References")]
    public GameObject leftWing;
    public GameObject rightWing;
    public GameObject tail;
    
    [Header("Optional UI References")]
    public Toggle leftWingToggle;
    public Toggle rightWingToggle;
    public Toggle tailToggle;
    
    void Start()
    {
        // Set up UI toggle listeners if they exist
        if (leftWingToggle != null)
        {
            leftWingToggle.isOn = leftWing != null && leftWing.activeSelf;
            leftWingToggle.onValueChanged.AddListener(isOn => ToggleLeftWing(isOn));
        }
        
        if (rightWingToggle != null)
        {
            rightWingToggle.isOn = rightWing != null && rightWing.activeSelf;
            rightWingToggle.onValueChanged.AddListener(isOn => ToggleRightWing(isOn));
        }
        
        if (tailToggle != null)
        {
            tailToggle.isOn = tail != null && tail.activeSelf;
            tailToggle.onValueChanged.AddListener(isOn => ToggleTail(isOn));
        }
    }
    
    void Update()
    {
        // Keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleLeftWing(!leftWing.activeSelf);
            if (leftWingToggle != null) leftWingToggle.isOn = leftWing.activeSelf;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleRightWing(!rightWing.activeSelf);
            if (rightWingToggle != null) rightWingToggle.isOn = rightWing.activeSelf;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleTail(!tail.activeSelf);
            if (tailToggle != null) tailToggle.isOn = tail.activeSelf;
        }
    }
    
    public void ToggleLeftWing(bool isActive)
    {
        if (leftWing != null)
        {
            leftWing.SetActive(isActive);
            Debug.Log($"Left Wing {(isActive ? "Enabled" : "Disabled")}");
        }
    }
    
    public void ToggleRightWing(bool isActive)
    {
        if (rightWing != null)
        {
            rightWing.SetActive(isActive);
            Debug.Log($"Right Wing {(isActive ? "Enabled" : "Disabled")}");
        }
    }
    
    public void ToggleTail(bool isActive)
    {
        if (tail != null)
        {
            tail.SetActive(isActive);
            Debug.Log($"Tail {(isActive ? "Enabled" : "Disabled")}");
        }
    }
}
