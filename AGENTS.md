# Deucarian Viewer Navigation Agent Notes

Package ID: `com.deucarian.viewer-navigation`

## Ownership

This package owns the reusable viewer navigation experience: authoritative Orbit/Fly
and top-down state, camera-action coordination, cancellable transitions, reference
bounds/pivot/origin lifecycle, navigation input blocking, toolbar presentation, and the
view cube.

It must not own camera math, raw input devices outside the Input System adapter,
pointer-lock platform code, command routing, WebGL transport, model loading, or report,
activity, and backend DTOs.

## Dependencies

Camera math remains in Camera Navigation. Raw Input System reading remains in its
integration package. Pointer-lock lifecycle remains in Pointer Capture. Runtime chrome
and palettes remain in UI and Theming. Operational state must remain registered with
Diagnostics and all logging must use Deucarian Logging. Editor surfaces must use
Deucarian Editor.

## Policies

- Preserve one authoritative state owner and event-driven presentation updates.
- Keep reference registration separate from visibility or selection updates.
- Never move the camera as a side effect of a model visibility change.
- New camera actions supersede active transitions; do not run parallel moves.
- Do not add direct Unity `Debug` calls or direct object-destruction helpers.
- Keep production files under 500 lines.
- Validate package metadata, EditMode/PlayMode tests, and `git diff --check`.
