# Changelog

## 0.1.1 - 2026-08-14

- Coordinated configured drag actions with the Pointer Capture Requested, Active,
  Rejected, and Lost lifecycle; navigation now remains blocked until capture is active
  and a lost or rejected held gesture cannot resume without release and rearming.
- Added an injectable renderer-bounds strategy so consumers can preserve their proven
  model framing policy without retaining a second navigation engine.

## 0.1.0 - 2026-08-13

- Added the viewer navigation composition root and authoritative state owner.
- Added Orbit, Fly, top-down, return-to-origin, framing, and cancellable transitions.
- Added pointer capture and explicit UI input-blocking coordination.
- Added a camera-following six-face view cube and themed runtime toolbar.
- Added Diagnostics, Deucarian Editor tooling, samples, and lifecycle/state tests.
- Routed viewer-level action observation through the configurable Input System
  integration, including remapped-control and input-blocking coverage.
- Preserved the active theme palette while camera orientation updates the view
  cube, and exposed the captured origin as read-only navigation state.
