# Project Changelog

## 2026-05-22 - Loading Screen Service Refactor

**Changed:** Split scene transition loading responsibilities into a smaller `SceneTransitionService`, a UI Toolkit-backed `SceneLoadingOverlay`, UXML/USS loading screen assets, and a focused `SceneUiInputSuspender`.

**Fixed:** Added an opaque fallback-color backing behind loading art so semi-transparent background pixels no longer reveal the scene behind the overlay.

**Documentation:** Updated `Loading_Screens.md` to describe the UI Toolkit ownership model and background backing behavior.
