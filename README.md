# Corner Shift Planter (Rust Plugin)

Corner Shift Planter allows players to plant seeds only in the four corner slots of a Large Planter Box while holding Shift, when enabled via a toggle command.

This plugin preserves vanilla Rust planting mechanics while giving players optional precision planting control.

---

## 🔹 Features

- Per-player toggle system
- Permission controlled
- Optional Shift requirement
- Auto-disable timer (configurable)
- Supports all vanilla seed types
- Optimized for high-population servers
- No reflection or deprecated API usage

---

## 🔹 Command

```

/corner

```

Toggles corner planting mode for the player.

---

## 🔹 Permission

```

cornershiftplanter.use

```

Grant to default group:
```

oxide.grant group default cornershiftplanter.use

```

Grant to VIP:
```

oxide.grant group vip cornershiftplanter.use

```

---

## 🔹 Configuration

Located at:

```

oxide/config/CornerShiftPlanter.json

````

Example:

```json
{
  "LargePlanterOnly": true,
  "RequireShift": true,
  "CenterThreshold": 0.2,
  "AutoDisableSeconds": 120
}
````

### Config Options Explained

**LargePlanterOnly**

* If true, only affects Large Planter Boxes.

**RequireShift**

* Player must hold Shift while planting.

**CenterThreshold**

* Controls how center/edge slots are detected.

**AutoDisableSeconds**

* Automatically disables corner mode after X seconds.
* Set to 0 to disable auto-off.

---

## 🔹 How It Works

When enabled:

1. Player types `/corner`
2. Player holds Shift
3. Rust plants normally (3x3 grid)
4. Plugin removes non-corner plants
5. Seeds are refunded automatically
6. Only 4 corner plants remain

---

## 🔹 Performance

* Dynamically subscribes to hooks
* No heavy scanning
* No LINQ allocations
* No reflection
* Safe for 300+ player servers

---

## 🔹 Compatibility

* Fully compatible with vanilla Rust
* Does not override growth mechanics
* Does not conflict with crop systems

---

## 🔹 Version

Current Version: 3.2.1

---

## 🔹 License

MIT License

```
