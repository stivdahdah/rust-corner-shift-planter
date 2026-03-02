## Overview

Corner Shift Planter allows players to plant seeds exclusively in the four corner slots of a Large Planter Box while holding Shift, when enabled via a simple toggle command.

This plugin enhances planting precision while preserving vanilla Rust behavior. It does not automate planting or override core mechanics — it simply filters out non-corner plants and refunds the seeds.

The system is permission-controlled, configurable, lightweight, and optimized for high-population servers.

---

## Key Features

* Per-player toggle using `/corner`
* Permission-controlled access
* Optional Shift key requirement
* Configurable center detection threshold
* Configurable auto-disable timer
* Supports all vanilla seed types
* Dynamic hook subscription for performance
* No reflection or deprecated API usage
* Clean memory handling on unload

---

## How It Works

When enabled:

1. Player types `/corner`
2. Player holds Shift while planting
3. Rust performs normal 3x3 planting
4. The plugin removes non-corner plants
5. The consumed seeds are automatically refunded
6. Only the four corner plants remain

The plugin does not modify world entities beyond the initial plant spawn and does not alter growth behavior.

---

## Commands

```
/corner
```

Toggles corner planting mode for the player.

If auto-disable is enabled, the mode will turn off automatically after the configured duration.

---

## Permission

```
cornershiftplanter.use
```

Grant to default group:

```
oxide.grant group default cornershiftplanter.use
```

Grant to VIP group:

```
oxide.grant group vip cornershiftplanter.use
```

---

## Configuration

Located at:

```
oxide/config/CornerShiftPlanter.json
```

Example configuration:

```json
{
  "LargePlanterOnly": true,
  "RequireShift": true,
  "CenterThreshold": 0.2,
  "AutoDisableSeconds": 120
}
```

### Configuration Options

**LargePlanterOnly**
If true, functionality applies only to Large Planter Boxes.

**RequireShift**
If true, players must hold Shift while planting.

**CenterThreshold**
Controls the tolerance used to detect center or edge slots.
Adjust only if Rust planting offsets change in future updates.

**AutoDisableSeconds**
Automatically disables corner planting after X seconds.
Set to `0` to disable auto-off behavior.

---

## Performance & Optimization

Corner Shift Planter is designed for minimal performance impact:

* Dynamically subscribes to hooks only when active
* Uses lightweight `NextTick` execution
* No heavy entity scanning
* No LINQ allocations
* No persistent data files
* Clears all timers and memory on unload

Suitable for high-population servers (200–300  players).

---

## Compatibility

* Fully compatible with vanilla Rust mechanics
* Does not override growth systems
* Does not modify entity prefabs
* Does not conflict with farming or crop plugins
* Safe to use alongside automation systems

---

## Why Choose Corner Shift Planter?

Unlike automation-based farming plugins that override planting mechanics or enforce layouts globally, this plugin:

* Preserves default Rust behavior
* Gives players optional control
* Requires explicit activation
* Maintains gameplay balance
* Avoids intrusive world modifications

It is a focused, minimal enhancement rather than a gameplay overhaul.

---

## Abuse & Safety Considerations

* Feature gated behind permission
* Requires explicit player activation
* Optional Shift enforcement
* Optional auto-disable timer
* No background processing when inactive
* No admin abuse potential

---

## License

MIT License

---

## Credits

Inspiration for this concept came from SHADOWFRAX, who mentioned the idea during a YouTube discussion about  planting in Rust.

This plugin is an independent implementation and is not affiliated with or endorsed by SHADOWFRAX.