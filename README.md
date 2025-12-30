# yokai-raising-game
A casual Yokai idle growing game for Android built with Unity.

## Buttons (Normal Daily Actions)
- Btn_Purify: Normal purification (daily care).
- Btn_Dango: Normal dango (daily energy recovery).

## Special Recovery Actions (Not Normal Buttons)
- Emergency Purification:
  - Used to recover from Mononoke state.
  - Separate from Btn_Purify.

- Special Triple Dango:
  - Used to recover from Near-Death state.
  - Separate from Btn_Dango.

## Character States Summary
### Normal
- Player regularly uses:
  - Btn_Purify to control Kegare.
  - Btn_Dango to recover Energy.

### Mononoke State
- Trigger: Kegare reaches MAX and is left unattended.
- Recovery: Emergency Purification (special action).

### Near-Death State
- Trigger: Energy reaches 0 and is left unattended.
- Recovery: Special Triple Dango (special reward action).
