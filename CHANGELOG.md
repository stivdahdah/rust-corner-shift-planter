# Changelog

## v1.2.0
Fixed
- Resolved timer compile issues in the uMod build system by switching to Oxide.Plugins.Timer.
- Corrected command handling so subcommands no longer accidentally toggle corner mode.
Changed
- Simplified command structure and removed /corner auto.
- Corner mode now always enforces corner-only planting automatically.
- Invalid planting slots are silently removed (no repeated chat spam).
Added
- /corner on and /corner off commands for explicit control of the mode.
- /corner guide command to display the recommended genetics planting layout.
- /corner help command to show all available commands.
Improved
- Cleaner localization keys and help messages.
- Better command validation (unknown commands now show help).
- Safer timer cleanup and player state handling.
- Improved enforcement logic for invalid planting positions.
Behavior
- Corner planting only applies to Large Planter Boxes (configurable).
- Players must hold SHIFT while planting when RequireShift is enabled.
- Seeds are refunded automatically if an invalid planting spot is removed.

Commands
/corner        Toggle corner planting mode
/corner on     Enable corner planting mode
/corner off    Disable corner planting mode
/corner guide  Show recommended genetics layout
/corner help   Show help
Permission
cornershiftplanter.use

## v1.1.0
- Correct Timer usage
- Cleaner config handling
- Better refund method separation
- Safer null checks
- Reviewer-friendly structure
- Version bumped to 1.1.0

## v1.0.2
- Fixed Description

## v1.0.1
- Added auto-disable timer
- Dynamic hook subscription
- Improved performance safety
- Improved config validation

## v1.0.0
- Initial stable release
