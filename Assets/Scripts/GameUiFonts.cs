using UnityEngine;
using TMPro;

/// <summary>
/// Resolves the project's gameplay UI font (LuckiestGuy) for runtime-created labels.
/// </summary>
public static class GameUiFonts
{
    private static TMP_FontAsset cachedFont;
    private static Material cachedMaterial;

    public static void Apply(TextMeshProUGUI target)
    {
        if (target == null)
            return;

        TMP_FontAsset font = Resolve();
        if (font == null)
            return;

        target.font = font;
        if (cachedMaterial != null)
            target.fontSharedMaterial = cachedMaterial;
        else if (font.material != null)
            target.fontSharedMaterial = font.material;
    }

    public static TMP_FontAsset Resolve()
    {
        if (cachedFont != null)
            return cachedFont;

        // Prefer the font already used by FlightHUD score distance text.
        FlightHUD[] huds = Resources.FindObjectsOfTypeAll<FlightHUD>();
        for (int i = 0; i < huds.Length; i++)
        {
            FlightHUD hud = huds[i];
            if (hud == null || !hud.gameObject.scene.IsValid())
                continue;
            if (hud.distanceText != null && hud.distanceText.font != null)
            {
                CacheFrom(hud.distanceText);
                return cachedFont;
            }
        }

        // Fallback: LuckiestGuy SDF assets used across CanvasUI.
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset luckiestAny = null;
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset font = fonts[i];
            if (font == null)
                continue;

            string name = font.name;
            if (name == "LuckiestGuy-Regular SDF 1")
            {
                cachedFont = font;
                cachedMaterial = font.material;
                return cachedFont;
            }

            if (luckiestAny == null && name.IndexOf("LuckiestGuy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                luckiestAny = font;
        }

        if (luckiestAny != null)
        {
            cachedFont = luckiestAny;
            cachedMaterial = luckiestAny.material;
            return cachedFont;
        }

        cachedFont = TMP_Settings.defaultFontAsset;
        if (cachedFont != null)
            cachedMaterial = cachedFont.material;
        return cachedFont;
    }

    private static void CacheFrom(TextMeshProUGUI source)
    {
        cachedFont = source.font;
        cachedMaterial = source.fontSharedMaterial != null
            ? source.fontSharedMaterial
            : (source.font != null ? source.font.material : null);
    }
}
