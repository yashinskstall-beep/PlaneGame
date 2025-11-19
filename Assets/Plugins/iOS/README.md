# iOS Haptic Feedback Plugin

This plugin provides native iOS haptic feedback support for Unity.

## Features

The plugin implements iOS Haptic Feedback API (available from iOS 10.0+) with the following feedback types:

### Impact Feedback
- **Light Impact** - Subtle haptic feedback for light interactions
- **Medium Impact** - Moderate haptic feedback for standard interactions
- **Heavy Impact** - Strong haptic feedback for significant interactions

### Selection Feedback
- **Selection** - Haptic feedback for UI selection changes (buttons, toggles, etc.)
- **Continuous** - Repeating haptic feedback for drag interactions (triggers every 100ms)

### Notification Feedback
- **Success** - Haptic feedback indicating a task completed successfully
- **Warning** - Haptic feedback indicating a warning
- **Error** - Haptic feedback indicating an error occurred

## Usage

The VibrationManager automatically uses the appropriate haptic feedback on iOS:

```csharp
// Button clicks use Selection feedback
VibrationManager.Instance.VibrateButtonClick();

// Short vibrations use Light Impact
VibrationManager.Instance.VibrateShort();

// Medium vibrations use Medium Impact
VibrationManager.Instance.VibrateMedium();

// Long vibrations use Heavy Impact
VibrationManager.Instance.VibrateLong();

// Custom durations map to appropriate impact levels
VibrationManager.Instance.VibrateCustom(150);

// Continuous vibration (for dragging interactions)
VibrationManager.Instance.StartContinuous(); // Start continuous haptic feedback
VibrationManager.Instance.Stop();            // Stop continuous haptic feedback
```

## Build Requirements

- iOS 10.0 or later
- The plugin will automatically be included when building for iOS
- No additional configuration needed

## How It Works

The plugin uses Unity's native plugin system:
1. The `.mm` file contains Objective-C++ code that bridges Unity C# with iOS APIs
2. C# code uses `DllImport` to call the native functions
3. Platform-specific compilation directives ensure the code only runs on iOS devices

## Compatibility

- **iOS Devices**: Full haptic feedback support on devices with Taptic Engine (iPhone 7 and later)
- **Older iOS Devices**: Gracefully degrades (no haptic feedback on unsupported devices)
- **iOS Simulator**: No haptic feedback in simulator
- **Unity Editor**: No haptic feedback in editor (platform-specific code is disabled)
