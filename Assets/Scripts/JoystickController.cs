using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Joystick Components")]
    public RectTransform joystickBG;
    public RectTransform joystickHandle;

    private Vector2 inputVector;

    private void Start()
    {
        // Hide the joystick at the start
        if (joystickBG != null)
        {
            joystickBG.gameObject.SetActive(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBG, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos.x = (pos.x / joystickBG.sizeDelta.x);
            pos.y = (pos.y / joystickBG.sizeDelta.y);

            inputVector = new Vector2(pos.x * 2, pos.y * 2);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Move handle visually
            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * (joystickBG.sizeDelta.x / 2),
                inputVector.y * (joystickBG.sizeDelta.y / 2));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystickBG == null) return;

        // Move the joystick to the touch position and show it
        joystickBG.position = eventData.position;
        joystickBG.gameObject.SetActive(true);
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (joystickBG == null) return;

        // Reset and hide the joystick
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickBG.gameObject.SetActive(false);
    }

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
}
