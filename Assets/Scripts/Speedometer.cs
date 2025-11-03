using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{
    [Header("References")]
    public Rigidbody target; // The plane's Rigidbody

    [Header("Settings")]
    public float maxSpeed = 200f; // Max speed in km/h for arrow rotation
    public float minSpeedArrowAngle = 130f;
    public float maxSpeedArrowAngle = -130f;

    [Header("UI")]
    public TextMeshProUGUI speedLabel;   // Speed text label
    public RectTransform arrow;          // Speedometer needle/arrow

    private float speed = 0f;

    void Update()
    {
        if (target == null) return;

        // Calculate speed in the plane’s forward direction (local velocity)
        float forwardSpeed = Vector3.Dot(target.velocity, target.transform.forward);
        speed = Mathf.Max(0f, forwardSpeed * 3.6f); // convert m/s → km/h, no negative values

        // Update text
        if (speedLabel != null)
            speedLabel.text = $"{(int)speed} km/h";

        // Update arrow rotation
        if (arrow != null)
        {
            float t = Mathf.Clamp01(speed / maxSpeed);
            float angle = Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, t);
            arrow.localEulerAngles = new Vector3(0, 0, angle);
        }
    }
}
