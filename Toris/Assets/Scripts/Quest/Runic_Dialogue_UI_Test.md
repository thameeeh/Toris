# Runic Dialogue UI Test

## Goal

Use the copied Pixel Crushers Runic dialogue UI as Toris' current dialogue UI test.

## Setup

- Use `Assets/Scripts/Quest/Prefabs/Standard UI Prefabs/Pro/Runic/Runic Standard Dialogue UI.prefab`.
- Assign it to the `Dialogue Manager` in `MainArea`.
- Keep Pixel Crushers in charge of dialogue flow, response buttons, continue buttons, and quest dialogue state.
- Do not add dialogue-specific logic to `InputManager`.

## First Test Rules

- Disable typewriter for now so one continue click advances one dialogue line.
- Leave the Runic quest tracker HUD out of scope for this first test.
- If the Runic UI works, keep it and only restyle later if needed.

## Test Checklist

- Guide dialogue opens with the Runic UI.
- Continue button advances dialogue.
- Player response buttons appear when the conversation has choices.
- Selecting a response continues the conversation.
- Existing quest accept, progress, turn-in, and reward flow still works.
