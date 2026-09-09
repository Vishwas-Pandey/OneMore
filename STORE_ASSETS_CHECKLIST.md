# Store Assets Checklist — Google Play

Specifications below reflect current (2025-era) Google Play Console requirements at time of writing. Play occasionally adjusts these — if Play Console's upload UI shows different numbers than listed here when you actually get to that step, trust the live UI over this document.

## Required assets

| Asset | Spec | Status |
|---|---|---|
| **App icon (store listing)** | 512 × 512 px, 32-bit PNG (with alpha channel), max 1MB | **DONE** — `StoreAssets/icon_512.png`. Downscaled from the same source used for the in-app launcher icon (`Assets/_Project/Sprites/AppIcon/icon_legacy_1024.png`). Note: this is a generic abstract mark (purple gradient, circle + motion lines) left over from before the game was renamed to "Flying Bird" — it doesn't depict the bird/torch theme. Functional and store-compliant as-is; consider commissioning a bird-themed icon later for stronger brand recognition. |
| **Feature graphic** | 1024 × 500 px, JPG or 24-bit PNG (no alpha) | **DONE** — `StoreAssets/feature_graphic.png`. Composited from an actual Main Menu capture (title art + volcanic background + tagline), not a fabricated mockup. |
| **Phone screenshots** | Minimum 2, maximum 8. JPG or 24-bit PNG (no alpha). Each dimension between 320px and 3840px. Max dimension ≤ 2× the min dimension. | **DONE (4 of the recommended 6)** — `StoreAssets/screenshots/`: `1_main_menu.png`, `2_gameplay.png` (mid-flight, torch pillar visible), `3_score_progress.png` (score-milestone popup), `4_game_over.png`. All captured from the actual signed release build (`OneMore_Release.aab`, installed via bundletool) running on a Pixel emulator, status bar and ad banner cropped out. "Increasing difficulty" and "High score/new-best" shots were not captured — reaching a high score requires sustained precise play that scripted taps couldn't reliably reproduce; capture these manually later if wanted, or the current 4 are sufficient (Play requires only 2 minimum). |
| **7-inch tablet screenshots** | Optional. Same format rules as phone. | Not required since this game is portrait-phone-oriented; skip unless you want tablet store presence. |
| **10-inch tablet screenshots** | Optional. | Same as above — skip unless targeting tablets. |
| **Promo video** | Optional. A YouTube URL, not an uploaded file. | Not created — optional, skip for initial launch. |
| **Short description** | ≤ 80 characters | **DONE** — see `PLAY_STORE_LISTING.md`. |
| **Full description** | ≤ 4000 characters | **DONE** — see `PLAY_STORE_LISTING.md`. |

## Where the files are

All final, ready-to-upload assets live in `StoreAssets/` at the project root:
```
StoreAssets/
  icon_512.png
  feature_graphic.png
  screenshots/
    1_main_menu.png
    2_gameplay.png
    3_score_progress.png
    4_game_over.png
```

## What NOT to do
- Do not upload a screenshot showing debug overlays, developer console text, or placeholder/lorem-ipsum content.
- Do not reuse the app icon art as a "screenshot" — Play's review has flagged this pattern before as non-representative of actual gameplay.
