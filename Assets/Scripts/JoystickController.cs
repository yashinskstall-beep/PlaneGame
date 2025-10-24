using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Joystick Components")]
    public RectTransform joystickBG;
    public RectTransform joystickHandle;

    [Header("Settings")]
    public float handleLimit = 100f; // how far handle can move
    private Vector2 inputVector;

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
                inputVector.x * (joystickBG.sizeDelta.x / 2) * 0.6f,
                inputVector.y * (joystickBG.sizeDelta.y / 2) * 0.6f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
}
