# Plane Controller System

This system provides control for the plane after it exits a ramp, giving it realistic flight physics and handling.

## Setup Instructions

1. **Add the required components to your plane GameObject:**
   - Attach the `PlaneController.cs` script
   - Attach the `PlaneEffects.cs` script (optional but recommended)
   - Make sure the plane has a Rigidbody component
   - Make sure the plane has a Collider component

2. **Configure the references:**
   - In the PlaneController component, assign the PlaneRampAligner reference
   - In the PlaneEffects component, assign any TrailRenderers, ParticleSystems, and AudioSource

3. **Set up visual effects (optional):**
   - Add TrailRenderers to the wing tips of your plane
   - Add ParticleSystems for engine and speed effects
   - Add an AudioSource for engine sounds

4. **Configure the tags:**
   - Make sure your ground objects are tagged with "Ground"

## How It Works

1. The plane is launched using the existing SimpleDragLauncher system
2. When the plane contacts a ramp, the PlaneRampAligner takes control
3. When the plane exits the ramp, the PlaneController takes over
4. The plane can now be controlled with keyboard input:
   - Arrow keys or WASD for steering
   - The plane will automatically maintain forward momentum
   - The plane will auto-level when no input is given

## Key Parameters

### PlaneController

- **Base Speed:** The minimum forward speed of the plane
- **Max Speed:** The maximum speed the plane can reach
- **Turn Speed:** How quickly the plane turns left/right
- **Bank Angle:** How much the plane banks when turning
- **Pitch Speed:** How quickly the plane pitches up/down
- **Auto Level:** Whether the plane automatically levels when no input is given

### PlaneEffects

- **Wing Trails:** Visual trails that appear behind the wings
- **Engine Effect:** Particle system for the engine
- **Speed Effect:** Particle system that activates at high speeds
- **Engine Audio:** Sound effects for the engine that change with speed

## Advanced Usage

- You can manually trigger the plane controller by calling `planeController.ForceControl()`
- You can disable the plane controller by calling `planeController.StopControlling()`
- You can simulate a crash by calling `planeEffects.OnCrash()`

## Troubleshooting

- If the plane doesn't respond to controls, check that `useKeyboardInput` is enabled
- If the plane doesn't take control after exiting a ramp, make sure the PlaneRampAligner is properly calling `ForceControl()`
- If the plane moves too fast or slow, adjust the speed parameters
- If the plane turns too sharply or slowly, adjust the turn speed and bank angle
