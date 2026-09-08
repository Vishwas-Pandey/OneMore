# Store Assets Checklist — Google Play

Specifications below reflect current (2025-era) Google Play Console requirements at time of writing. Play occasionally adjusts these — if Play Console's upload UI shows different numbers than listed here when you actually get to that step, trust the live UI over this document.

## Required assets

| Asset | Spec | Status |
|---|---|---|
| **App icon (store listing)** | 512 × 512 px, 32-bit PNG (with alpha channel), max 1MB | **NOT YET CREATED** — this is separate from the in-app launcher icon already wired into the build (`Assets/_Project/Sprites/AppIcon/icon_legacy_1024.png`). You can export a 512×512 PNG from that same source file (it's 1024×1024, just downscale it) rather than designing a new one. |
| **Feature graphic** | 1024 × 500 px, JPG or 24-bit PNG (no alpha) | **NOT YET CREATED** — displayed at the top of your Play Store listing page. Should showcase the game visually (a gameplay screenshot composited with the app name works well) — this needs actual gameplay footage/screenshots to look right, not just the icon art. |
| **Phone screenshots** | Minimum 2, maximum 8. JPG or 24-bit PNG (no alpha). Each dimension between 320px and 3840px. Max dimension ≤ 2× the min dimension (i.e. can't be extremely elongated). | **NOT YET CREATED** — requires a running build on a device/emulator (see `DEVICE_TEST_CHECKLIST.md`). Recommended set (matches `PLAY_STORE_LISTING.md`'s caption list): Main Menu, Gameplay, Increasing Difficulty, High Score, Game Over, Retry/Revive. |
| **7-inch tablet screenshots** | Optional. Same format rules as phone. | Not required since this game is portrait-phone-oriented; skip unless you want tablet store presence. |
| **10-inch tablet screenshots** | Optional. | Same as above — skip unless targeting tablets. |
| **Promo video** | Optional. A YouTube URL, not an uploaded file. | Not created — optional, skip for initial launch. |
| **Short description** | ≤ 80 characters | **DONE** — see `PLAY_STORE_LISTING.md`. |
| **Full description** | ≤ 4000 characters | **DONE** — see `PLAY_STORE_LISTING.md`. |

## Notes on capturing real screenshots

Once you have the app running on a device (physical or emulator) per `DEVICE_TEST_CHECKLIST.md`:
- Use the device's own screenshot function (volume-down + power on most Android phones) rather than a screen-recording frame grab — you want full resolution, not a compressed video frame.
- Capture in portrait orientation (this game is portrait-locked).
- Play Console accepts screenshots with rounded device-frame mockups or plain raw captures — plain raw captures are simplest and always acceptable; device-frame mockups are a cosmetic choice, not a requirement.

## What NOT to do
- Do not upload a screenshot showing debug overlays, developer console text, or placeholder/lorem-ipsum content.
- Do not reuse the app icon art as a "screenshot" — Play's review has flagged this pattern before as non-representative of actual gameplay.
