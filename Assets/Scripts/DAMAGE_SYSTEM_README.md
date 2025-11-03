# Plane Damage System

This system allows the plane to react differently when specific parts (wings, tail) are disabled.

## Setup Instructions

1. **Add the PlaneDamageHandler component to your Airplane GameObject**
   - The Airplane GameObject should already have the PlaneController component
   - In the Inspector, drag the appropriate child GameObjects to the part references:
     - Left Wing → Left_Wing GameObject
     - Right Wing → Right_Wing GameObject
     - Tail → Tail GameObject

2. **Configure the damage effect settings:**
   - `wingDamageRollMultiplier`: How much faster the plane will roll when a wing is disabled
   - `tailDamagePitchMultiplier`: How much faster the plane will pitch down when the tail is disabled
   - `additionalDragPerMissingPart`: Additional drag applied when parts are missing

3. **Link the PlaneDamageHandler to the PlaneController**
   - In the PlaneController component, drag the PlaneDamageHandler component to the "Damage Handler" field

## Testing the Damage System

### Option 1: Using the PlanePartToggler with keyboard shortcuts
1. Add the PlanePartToggler script to a GameObject in your scene (like the Main Camera or UI Canvas)
2. Assign the plane parts in the Inspector
3. Use the following keyboard shortcuts during gameplay:
   - Press `1` to toggle the left wing
   - Press `2` to toggle the right wing
   - Press `3` to toggle the tail

### Option 2: Using UI Toggles
1. Create UI Toggle elements for each part
2. Assign them to the PlanePartToggler component
3. Click the toggles during gameplay to enable/disable parts

## Behavior Changes

When parts are disabled, the plane will behave as follows:

- **Left Wing Disabled**: The plane will roll left (counter-clockwise) faster when turning left
- **Right Wing Disabled**: The plane will roll right (clockwise) faster when turning right
- **Tail Disabled**: The plane will pitch down faster and be harder to control

Each disabled part also adds drag to the plane, making it slower overall.

## Extending the System

You can extend this system by:
1. Adding more parts (like engines, flaps, etc.)
2. Creating visual effects when parts are disabled
3. Adding health to each part instead of just on/off states
