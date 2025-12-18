# Kinect Power-Cycling Fix Guide

## Problem Summary

**Symptoms:**
- On restart, Kinect lights reset (power-cycles)
- After restart, character continues but Kinect tracking stops working
- Sometimes Kinect is "on" but body frames aren't received

**Root Cause:**
- `sensor.Close()` called in `OnDestroy()` → triggers on every scene reload
- Each restart closes/reopens Kinect → causes initialization delays
- Body frames become null during reboot → rotation becomes identity → no control

---

## A) QUICK FIX (Already Applied ✅)

### What Was Changed:

**Before:**
```csharp
void OnDestroy()
{
    if (bodyFrameReader != null)
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
    }

    if (sensor != null && sensor.IsOpen)
    {
        sensor.Close();  // ❌ THIS CAUSES POWER-CYCLING
        sensor = null;
    }
}
```

**After:**
```csharp
void OnDestroy()
{
    // Don't close the sensor on scene reload - this causes power-cycling!
    // Only dispose the reader (it's recreated on next scene load)
    if (bodyFrameReader != null)
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
    }

    // Clear references but DON'T close sensor
    sensor = null;
    bodies = null;
}

void OnApplicationQuit()
{
    // Only close sensor when application is actually quitting
    if (bodyFrameReader != null)
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
    }

    if (sensor != null && sensor.IsOpen)
    {
        sensor.Close();  // ✅ Only closes on app quit
        sensor = null;
    }
}
```

**Files Modified:**
- ✅ `KinectPlayerMovemen.cs` - Lines 268-297
- ✅ `KinectControler.cs` - Lines 243-272

**Result:** Sensor stays open across scene reloads. No more power-cycling!

---

## B) PROPER FIX (Recommended)

### Use Persistent `KinectSensorManager`

**Goal:** Open Kinect once, keep it alive across scenes, gameplay scripts only *read*.

### Step 1: Update `KinectPlayerMovemen.cs`

**Replace `InitializeKinect()` and `Start()`:**

**Before:**
```csharp
void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponentInChildren<Animator>();

    // Initialize Kinect
    InitializeKinect();

    // Start waiting for movement detection
    StartCoroutine(WaitForMovementOrTimeout());
}

private void InitializeKinect()
{
    sensor = KinectSensor.GetDefault();
    if (sensor != null)
    {
        if (!sensor.IsOpen)
        {
            sensor.Open();
        }
        bodyFrameReader = sensor.BodyFrameSource.OpenReader();
        bodies = new Body[sensor.BodyFrameSource.BodyCount];
    }
}
```

**After:**
```csharp
private bool usingManager = false; // Track if using manager

void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponentInChildren<Animator>();

    // PROPER FIX: Initialize from persistent manager
    StartCoroutine(InitFromManager());
}

private IEnumerator InitFromManager()
{
    // Wait for KinectSensorManager to be ready
    yield return KinectSensorManager.Instance.WaitForReady();

    // Get references from the manager (shared resources)
    sensor = KinectSensorManager.Instance.Sensor;
    bodyFrameReader = KinectSensorManager.Instance.BodyFrameReader;
    bodies = KinectSensorManager.Instance.Bodies;
    usingManager = true;

    Debug.Log("[KinectPlayerMovement] Using KinectSensorManager (persistent).");

    // Start waiting for movement detection
    StartCoroutine(WaitForMovementOrTimeout());
}
```

**Update `OnDestroy()`:**

**Before:**
```csharp
void OnDestroy()
{
    if (bodyFrameReader != null)
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
    }
    sensor = null;
    bodies = null;
}
```

**After:**
```csharp
void OnDestroy()
{
    // PROPER FIX: Don't dispose/close shared Kinect resources from manager
    if (usingManager)
    {
        // Just clear references - manager owns the resources
        bodyFrameReader = null;
        sensor = null;
        bodies = null;
        Debug.Log("[KinectPlayerMovement] Cleared references (using shared manager resources).");
    }
    else
    {
        // Fallback: dispose only if we created our own reader
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }
        sensor = null;
        bodies = null;
    }
}
```

### Step 2: Apply Same Changes to `KinectControler.cs`

Apply identical changes to `KinectControler.cs` (same pattern).

### Step 3: Ensure `KinectSensorManager` Closes on App Quit

**File:** `KinectSensorManager.cs`

**Add:**
```csharp
void OnApplicationQuit()
{
    // Ensure sensor is closed when application quits
    Cleanup();
}
```

**Update `Cleanup()`:**
```csharp
private void Cleanup()
{
    Debug.Log("[KinectSensorManager] Cleaning up Kinect resources...");
    
    if (bodyFrameReader != null)
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
    }

    // Close sensor only when application is quitting (not on scene changes)
    if (sensor != null && sensor.IsOpen)
    {
        sensor.Close();
        Debug.Log("[KinectSensorManager] Kinect sensor closed.");
    }

    IsInitialized = false;
    IsReady = false;
}
```

---

## C) CODE PATCHES SUMMARY

### Patch 1: `KinectPlayerMovemen.cs` - Use Manager

**Location:** Replace `InitializeKinect()` method and update `Start()`

**Add field:**
```csharp
private bool usingManager = false;
```

**Replace `Start()`:**
```csharp
void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponentInChildren<Animator>();

    // Initialize from persistent manager
    StartCoroutine(InitFromManager());
}

private IEnumerator InitFromManager()
{
    yield return KinectSensorManager.Instance.WaitForReady();

    sensor = KinectSensorManager.Instance.Sensor;
    bodyFrameReader = KinectSensorManager.Instance.BodyFrameReader;
    bodies = KinectSensorManager.Instance.Bodies;
    usingManager = true;

    Debug.Log("[KinectPlayerController] Using KinectSensorManager.");

    StartCoroutine(WaitForMovementOrTimeout());
}
```

**Update `OnDestroy()`:**
```csharp
void OnDestroy()
{
    if (usingManager)
    {
        // Don't dispose shared resources
        bodyFrameReader = null;
        sensor = null;
        bodies = null;
    }
    else
    {
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }
        sensor = null;
        bodies = null;
    }
}
```

### Patch 2: `KinectControler.cs` - Same Changes

Apply identical patches to `KinectControler.cs`.

---

## D) DEBUG OVERLAY

### Enhanced `KinectDebugOverlay.cs`

**Already Created:** ✅ `KinectDebugOverlay.cs`

**Features:**
- Shows `sensor.IsOpen`
- Shows `sensor.IsAvailable`
- Shows `trackedBodiesCount`
- Shows if using `KinectSensorManager`
- Shows if frames are available
- Status indicator (✓/⚠/✗)

**Usage:**
1. Add `KinectDebugOverlay` component to any GameObject in game scene
2. Toggle `showDebug` in inspector
3. View real-time Kinect status in top-left corner

**What It Shows:**
```
=== KINECT STATUS ===
Manager: KinectSensorManager (persistent)
Sensor: Found
IsOpen: True
IsAvailable: True
Reader: OK
Tracked Bodies: 1
Frames Available: YES

✓ KINECT IS READING YOU
```

---

## TESTING CHECKLIST

### Quick Fix Test:
- [x] Restart game → Kinect lights should NOT reset
- [x] After restart → Character should respond to Kinect immediately
- [x] Multiple restarts → Should work consistently

### Proper Fix Test:
- [ ] Restart game → Kinect stays open (no power-cycle)
- [ ] Check debug overlay → Shows "Manager: KinectSensorManager"
- [ ] After restart → "✓ KINECT IS READING YOU" appears quickly
- [ ] Multiple restarts → No delays, consistent behavior

---

## MIGRATION PATH

### Option 1: Keep Quick Fix (Current State)
- ✅ Already applied
- ✅ Works immediately
- ⚠️ Still creates new readers per scene (minor overhead)

### Option 2: Migrate to Proper Fix
1. Update `KinectPlayerMovemen.cs` with `InitFromManager()`
2. Update `KinectControler.cs` with `InitFromManager()`
3. Ensure `KinectSensorManager` exists in scene (or gets created automatically)
4. Test restart behavior

**Recommendation:** Migrate to Proper Fix for best long-term stability.

---

## TROUBLESHOOTING

### Issue: "KinectSensorManager.Instance is null"
**Solution:** Ensure `KinectSensorManager` script exists. It auto-creates on first access.

### Issue: "Still seeing power-cycling"
**Check:**
1. Verify `OnDestroy()` doesn't call `sensor.Close()`
2. Check if other scripts are closing sensor
3. Verify `OnApplicationQuit()` is only place that closes sensor

### Issue: "Frames still null after restart"
**Check:**
1. Wait time might be too short (increase `WaitForMovementOrTimeout` timeout)
2. Kinect might need more time to warm up
3. Check debug overlay to see actual status

---

## SUMMARY

**Quick Fix:** ✅ Applied - Sensor no longer closes on scene reload
**Proper Fix:** 📝 Ready to apply - Use `KinectSensorManager` singleton
**Debug Tool:** ✅ Created - `KinectDebugOverlay.cs` for real-time status

**Result:** Restart should NOT power-cycle Kinect and tracking should work reliably!

