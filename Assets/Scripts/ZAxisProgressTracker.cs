using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Tracks player progress along the Z-axis between two points and updates a UI slider.
/// </summary>
public class ZAxisProgressTracker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform pointA;
    public Transform pointB;
    public Slider slider;
    public Text percentText; // optional
    #if TMP_PRESENT
    public TextMeshProUGUI percentTMP; // optional TMP
    #endif

    [Header("Smooth Options")]
    public bool smooth = true;
    public float smoothSpeed = 10f;

    private float displayedValue = 0f;
    private float startZ;
    private float endZ;

    void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("Assign both PointA and PointB in inspector!");
            enabled = false;
            return;
        }

        startZ = pointA.position.z;
        endZ = pointB.position.z;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
        }
    }

    void Update()
    {
        if (player == null || slider == null) return;

        float playerZ = player.position.z;

        // Calculate normalized progress (0 → 1)
        float progress = Mathf.InverseLerp(startZ, endZ, playerZ);

        // Smooth fill movement
        if (smooth)
            displayedValue = Mathf.Lerp(displayedValue, progress, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        else
            displayedValue = progress;

        slider.value = displayedValue;

        UpdateText(displayedValue);
    }

    void UpdateText(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        if (percentText != null)
            percentText.text = percent + "%";
        #if TMP_PRESENT
        if (percentTMP != null)
            percentTMP.text = percent + "%";
        #endif
    }
}
