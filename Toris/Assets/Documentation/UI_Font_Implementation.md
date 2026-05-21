# UI Font Implementation

## Goal
Use the existing pixel fonts for game text without adding new runtime UI logic.

## Font Roles
- `VT323-Regular.ttf` is the default UI font for readable body text, inventory labels, tooltips, dialogue-style text, and general interface copy.
- `bitcell_memesbruh03.ttf` is the accent/display font for titles, buttons, tabs, HUD labels, hotkeys, and short emphasis text.

## Assets
- UI Toolkit font source: `Assets/Art/PixelArtGUI/Fonts/VT323-Regular.ttf`
- UI Toolkit display font source: `Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03.ttf`
- TextMesh Pro body font asset: `Assets/Art/PixelArtGUI/Fonts/VT323-Regular SDF.asset`
- TextMesh Pro display font asset: `Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03 SDF.asset`

## Implementation
1. Apply `VT323-Regular.ttf` globally through `Assets/UI_Toolkit/USS/theme-variables.uss`.
2. Apply `bitcell_memesbruh03.ttf` to reusable display classes in shared and screen USS files.
3. Calibrate UI type around the project's 32x32 pixel-art baseline: small labels around 24px, readable body text around 32px, and display text on larger 48px/72px/96px steps.
4. Keep all layout and font styling in USS where possible, following the project UI Toolkit convention.
5. Use the existing TMP SDF assets for Canvas/TextMeshPro text that cannot inherit UI Toolkit styles.

## Verification
- Open the HUD, inventory, smith, skill, pause, main menu, and death screens in Unity.
- Check for clipped text, especially HUD overlays, small item quantities, skill nodes, and buttons.
- Confirm body text stays readable and display text has the intended pixel-art style.
