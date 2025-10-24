# Plane Handling Controller

This script provides control over the plane's handling (turning, banking, and pitching) after it exits a ramp, without affecting its speed or other physics properties.

## Setup Instructions

1. **Add the required components to your plane GameObject:**
   - Attach the `PlaneController.cs` script
   - Make sure the plane has a Rigidbody component
   - Make sure the plane has a Collider component

2. **Configure the references:**
   - In the PlaneController component, assign the PlaneRampAligner reference

3. **Configure the tags:**
   - Make sure your ground objects are tagged with "Ground"

## How It Works

1. The plane is launched using the existing SimpleDragLauncher system
2. When the plane contacts a ramp, the PlaneRampAligner takes control
3. When the plane exits the ramp, the PlaneController takes over handling only
4. The plane can now be steered with keyboard input:
   - Left/Right Arrow keys: Turn the plane left/right (with banking)
   - Up/Down Arrow keys: Pitch the plane up/down
   - No input: The plane will auto-level its roll

## Key Parameters

### Handling Settings

- **Turn Speed:** How quickly the plane turns left/right
- **Bank Angle:** How much the plane banks when turning
- **Pitch Speed:** How quickly the plane pitches up/down
- **Max Pitch Angle:** Maximum angle the plane can pitch up/down

### Input Settings

- **Use Keyboard Input:** Whether to use keyboard input for control
- **Horizontal Input Sensitivity:** How sensitive the turning is to left/right input
- **Vertical Input Sensitivity:** How sensitive the pitching is to up/down input
- **Auto Level When No Input:** Whether the plane automatically levels when no input is given
- **Auto Level Speed:** How quickly the plane auto-levels

## Important Notes

- This controller only affects the plane's rotation (handling) and does not modify its speed or velocity
- The original physics and momentum of the plane are preserved
- The controller will automatically disable when the plane collides with the ground or other objects

## Troubleshooting

- If the plane doesn't respond to controls, check that `useKeyboardInput` is enabled
- If the plane doesn't take control after exiting a ramp, make sure the PlaneRampAligner is properly calling `ForceControl()`
- If the plane turns too sharply or slowly, adjust the turn speed and bank angle
