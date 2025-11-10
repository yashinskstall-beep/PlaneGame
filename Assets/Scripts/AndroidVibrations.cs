using UnityEngine;

public static class AndroidVibrations
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static bool isVibrating = false;

    static AndroidVibrations()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Vibrator init failed: " + e.Message);
        }
    }

    public static void StartContinuous()
    {
        if (vibrator == null || isVibrating) return;

        // Pattern: delay, vibrate, sleep, repeat index (0 means repeat forever)
        long[] pattern = { 0, 50, 50 };
        vibrator.Call("vibrate", pattern, 0);
        isVibrating = true;
        Debug.Log("Vibration started");
    }

    public static void Stop()
    {
        if (vibrator == null || !isVibrating) return;
        vibrator.Call("cancel");
        isVibrating = false;
        Debug.Log("Vibration stopped");
    }
#else
    public static void StartContinuous() => Debug.Log("Vibration Start (Editor)");
    public static void Stop() => Debug.Log("Vibration Stop (Editor)");
#endif
}
