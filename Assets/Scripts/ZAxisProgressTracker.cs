using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tracks player progress along the Z-axis between two points and updates the
/// right-side DistanceSlider, including a percentage label that rides with the handle.
/// </summary>
public class ZAxisProgressTracker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform pointA;
    public Transform pointB;
    public Slider slider;
    public Text percentText;
    public TextMeshProUGUI percentTMP;

    [Header("Percent Label")]
    [Tooltip("Create a % label automatically if none is assigned.")]
    public bool autoCreatePercentLabel = true;

    [Tooltip("Screen-space offset from the slider handle (left of the bar).")]
    public Vector2 percentOffset = new Vector2(-55f, 0f);

    public float percentFontSize = 40f;
    public Color percentColor = Color.white;
    public Color percentOutlineColor = Color.black;
    public float percentOutlineWidth = 0.35f;
    public Vector2 percentShadowDistance = new Vector2(2.5f, -2.5f);

    [Header("Smooth Options")]
    public bool smooth = true;
    public float smoothSpeed = 10f;

    private float displayedValue = 0f;
    private float startZ;
    private float endZ;
    private bool pointsReady;
    private RectTransform percentRect;
    private RectTransform handleRect;
    private bool percentVisible;

    void Start()
    {
        ResolveSlider();
        CachePoints();
        EnsurePercentLabel();
        ResetVisuals();
    }

    void Update()
    {
        if (player == null)
            return;

        if (slider == null)
        {
            ResolveSlider();
            if (slider == null)
                return;
            EnsurePercentLabel();
        }

        if (!pointsReady)
            CachePoints();
        if (!pointsReady)
            return;

        float playerZ = player.position.z;
        float progress = Mathf.Clamp01(Mathf.InverseLerp(startZ, endZ, playerZ));

        if (smooth)
            displayedValue = Mathf.Lerp(displayedValue, progress, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        else
            displayedValue = progress;

        slider.value = displayedValue;
        UpdatePercentText(displayedValue);
    }

    void LateUpdate()
    {
        // After UI layout so the label never flashes at canvas-center before the handle moves.
        UpdatePercentPosition();
    }

    private void CachePoints()
    {
        if (pointA == null || pointB == null)
        {
            pointsReady = false;
            return;
        }

        startZ = pointA.position.z;
        endZ = pointB.position.z;
        pointsReady = true;
    }

    private void ResolveSlider()
    {
        if (slider != null)
            return;

        Slider[] allSliders = Resources.FindObjectsOfTypeAll<Slider>();
        for (int i = 0; i < allSliders.Length; i++)
        {
            Slider candidate = allSliders[i];
            if (candidate == null || candidate.name != "DistanceSlider")
                continue;
            if (!candidate.gameObject.scene.IsValid())
                continue;

            slider = candidate;
            break;
        }
    }

    private void EnsurePercentLabel()
    {
        if (percentTMP != null)
        {
            percentRect = percentTMP.rectTransform;
            ApplyPercentHighlight(percentTMP);
            CacheHandle();
            SetPercentVisible(false);
            return;
        }

        if (percentText != null)
        {
            percentRect = percentText.rectTransform;
            CacheHandle();
            SetPercentVisible(false);
            return;
        }

        if (!autoCreatePercentLabel || slider == null)
            return;

        Transform existing = slider.transform.Find("ProgressPercent");
        if (existing == null && slider.handleRect != null)
            existing = slider.handleRect.Find("ProgressPercent");

        if (existing == null && slider.transform.parent != null)
            existing = slider.transform.parent.Find("ProgressPercent");

        if (existing != null)
        {
            percentTMP = existing.GetComponent<TextMeshProUGUI>();
            if (percentTMP != null)
            {
                percentRect = percentTMP.rectTransform;
                ApplyPercentHighlight(percentTMP);
                CacheHandle();
                SetPercentVisible(false);
                return;
            }
        }

        // Parent to the flight canvas so text stays upright (slider itself is rotated 90°).
        Transform parent = slider.transform.parent != null ? slider.transform.parent : slider.transform;
        GameObject go = new GameObject("ProgressPercent", typeof(RectTransform));
        go.layer = slider.gameObject.layer;
        go.transform.SetParent(parent, false);

        percentRect = go.GetComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.5f, 0.5f);
        percentRect.anchorMax = new Vector2(0.5f, 0.5f);
        percentRect.pivot = new Vector2(1f, 0.5f);
        percentRect.sizeDelta = new Vector2(140f, 56f);
        percentRect.localRotation = Quaternion.identity;
        percentRect.localScale = Vector3.one;

        percentTMP = go.AddComponent<TextMeshProUGUI>();
        percentTMP.text = "0%";
        percentTMP.alignment = TextAlignmentOptions.MidlineRight;
        percentTMP.enableWordWrapping = false;
        percentTMP.raycastTarget = false;
        percentTMP.overflowMode = TextOverflowModes.Overflow;
        ApplyPercentHighlight(percentTMP);

        CacheHandle();
        SetPercentVisible(false);
        Canvas.ForceUpdateCanvases();
        UpdatePercentPosition();
    }

    private void ApplyPercentHighlight(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        GameUiFonts.Apply(tmp);
        tmp.fontSize = percentFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = percentColor;
        tmp.enableVertexGradient = false;

        // Thick TMP outline + soft underlay so white % pops on sky/trees.
        tmp.fontMaterial = tmp.fontMaterial;
        tmp.outlineColor = percentOutlineColor;
        tmp.outlineWidth = Mathf.Clamp01(percentOutlineWidth);

        if (tmp.fontMaterial != null)
        {
            Material mat = tmp.fontMaterial;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, percentOutlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, percentOutlineColor);

            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.35f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
        }

        // Extra UI outline/shadow for readability even if TMP outline is weak on this font.
        Outline uiOutline = tmp.GetComponent<Outline>();
        if (uiOutline == null)
            uiOutline = tmp.gameObject.AddComponent<Outline>();
        uiOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        uiOutline.effectDistance = percentShadowDistance;
        uiOutline.useGraphicAlpha = true;

        Shadow plainShadow = null;
        Shadow[] shadows = tmp.GetComponents<Shadow>();
        for (int i = 0; i < shadows.Length; i++)
        {
            if (shadows[i] != null && shadows[i].GetType() == typeof(Shadow))
            {
                plainShadow = shadows[i];
                break;
            }
        }

        if (plainShadow == null)
            plainShadow = tmp.gameObject.AddComponent<Shadow>();

        plainShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        plainShadow.effectDistance = percentShadowDistance * 1.5f;
        plainShadow.useGraphicAlpha = true;
    }

    private void CacheHandle()
    {
        handleRect = slider != null ? slider.handleRect : null;
    }

    private void ResetVisuals()
    {
        displayedValue = 0f;
        percentVisible = false;
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
        }

        UpdatePercentText(0f);
        SetPercentVisible(false);
        Canvas.ForceUpdateCanvases();
        UpdatePercentPosition();
    }

    private void UpdatePercentText(float value)
    {
        int percent = Mathf.Clamp(Mathf.RoundToInt(value * 100f), 0, 100);
        string text = percent + "%";

        if (percentText != null)
            percentText.text = text;
        if (percentTMP != null)
            percentTMP.text = text;
    }

    private void UpdatePercentPosition()
    {
        if (percentRect == null)
            return;

        if (handleRect == null && slider != null)
            handleRect = slider.handleRect;

        if (handleRect == null || !handleRect.gameObject.activeInHierarchy)
            return;

        // Keep label next to the moving plane/handle icon, slightly toward screen center.
        Vector3 handlePos = handleRect.position;
        percentRect.position = handlePos + new Vector3(percentOffset.x, percentOffset.y, 0f);
        percentRect.SetAsLastSibling();

        if (!percentVisible)
            SetPercentVisible(true);
    }

    private void SetPercentVisible(bool visible)
    {
        percentVisible = visible;

        if (percentTMP != null)
            percentTMP.enabled = visible;
        if (percentText != null)
            percentText.enabled = visible;

        if (percentRect != null && percentRect.gameObject.activeSelf != visible)
            percentRect.gameObject.SetActive(visible);
    }
}
