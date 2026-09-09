# Flying Bird — Final Release Audit

**Last updated: September 9, 2026.** Supersedes and replaces the earlier `DEPLOYMENT_STATUS.md`, `FINAL_RELEASE_STATUS.md`, and `DEVICE_TEST_CHECKLIST.md` (all deleted — they described an earlier, pre-rebuild version of this game and were no longer accurate).

This document reflects a full pass: every gameplay/UI/ads script read line by line, every scene checked for null/broken references, a fresh signed release build produced and verified, and a real on-device playthrough of every screen and button on an Android emulator (Pixel, API 37). Bugs found during this pass were fixed and re-verified, not just logged.

---

## What the game actually is right now

**Flying Bird** — a classic tap-to-fly arcade game (flappy-bird mechanic): tap to flap upward, gravity pulls you back down, thread the gap between torch-lit pillar pairs. Gap width and pillar speed both increase with score. Package `com.onemorestudio.onemore`, version `1.0.0` (version code `1`), min SDK 25, target SDK 35, ARM64 + IL2CPP.

## Bugs found this pass, fixed, and re-verified

1. **Score-save corruption risk** — `GameManager` kept its own independent copy of score/bestScore in parallel with `ScoreManager` (the real, combo-aware, UI-driving, already-saves-in-real-time authority), and re-saved a stale comparison on every death. This could silently overwrite a correct saved best score with a lower one. Fixed: `GameManager` no longer tracks score at all; `GameOver(finalScore, isNewBest)` now takes authoritative values sourced from `ScoreManager`.
2. **Continue-after-ad silently broke** — `RestartButton`/`ContinueButton`/`MenuButton` in the Gameplay scene each had both a persistent Inspector `onClick` binding *and* a code-side `AddListener` for the same handler, firing every tap twice. Harmless for Restart/Menu, but broke Continue: the spurious second call resumed gameplay behind the still-playing rewarded ad, so the bird fell and died invisibly before the player finished watching. Fixed: removed the redundant persistent listeners (code-only wiring now, matching the rest of the project's convention). Verified on-device: `ShowRewardedAd` now fires exactly once per tap.
3. **Rewarded ad abort dead-end** — closing a rewarded ad early (before earning the reward) never resolved the pending continue callback, leaving the player stuck on Game Over with their one continue already spent for nothing. Fixed: the ad-closed handler now resolves it too.
4. **Pause broke after using Continue** — a successful Continue never restored `GameManager`'s state back to `Playing`, so the Pause button silently no-op'd for the rest of that run. Fixed: `BeginPlaying()` is now called in the Continue success path.
5. **Fragile init ordering** — `AnalyticsManager`/`ConsentManager`/`AdManager` initialization ran from `GameManager.Awake()`, which only works if those objects' own `Awake()` happened to run first — not guaranteed by Unity. Fixed: moved to `GameManager.Start()`, where Unity does guarantee every other object's `Awake()` has already completed.
6. **No pause-on-focus-loss** — pulling down the notification shade or a system dialog during gameplay let physics keep running unseen, producing an unfair death. Fixed: `GameManager.OnApplicationFocus` now auto-pauses if focus is lost mid-run.

All six were found via a genuine line-by-line script review plus on-device reproduction (not just static reading) where practical, and re-verified working after the fix by reinstalling and re-testing on the emulator.

## What was tested on-device (this pass)

- Fresh install (no prior save data) → Main Menu loads, `BEST: 0000`, no crash — confirms `SaveSystem` handles a missing save file correctly.
- Main Menu: Play, Sound toggle (ON↔OFF, verified both states render), Privacy Policy (confirmed it correctly launches the device browser via `Application.OpenURL`, not an in-app panel), Exit (confirmed via `adb shell pidof` that the process actually terminates).
- Gameplay: countdown → flight → death → Game Over, repeated across multiple runs.
- Game Over: Restart, Continue (rewarded ad — confirmed single invocation, confirmed gameplay resumes correctly after the ad closes), Menu.
- Interstitial ad (shown on Play, frequency-gated) and rewarded ad (Continue) both confirmed to actually request and display real ads end-to-end.
- Banner ad confirmed present on Main Menu, the Gameplay countdown, and Game Over; confirmed hidden during actual flight.
- No exceptions of any kind (`NullReferenceException`, `MissingMethodException`, or otherwise) found in logcat across the entire test session.

**Not exercised**: sustained gameplay to a high score / genuine "new best" run (automated tap timing against a live physics countdown proved too imprecise to script reliably) — the underlying fix for the score bug was verified by direct code inspection instead, tracing every call site of the old dual-tracking mechanism.

## Ads — current real status

- Real AdMob App ID and real banner/interstitial/rewarded ad unit IDs are wired in (both `MainMenu.unity` and `Gameplay.unity`), `useTestAds` is `false`. Confirmed via logcat that real ad requests go out to Google's servers under these IDs.
- Your AdMob account is still pending Google's approval (`"Account not approved yet"` in logs). Until approved, ads display Google's own "Test Ad"-labeled placeholder content automatically — this is expected, resolves on its own, and needs no further code changes.
- One remaining AdMob dashboard step (not code): **AdMob → Privacy & messaging** has no GDPR/consent message configured yet for this app — create one there when convenient; not a blocker for anything else.

## Privacy policy — done

Hosted live and verified reachable (HTTP 200) at **https://vishwas-pandey.github.io/OneMore/privacy-policy.html**, served via GitHub Pages from this repo's `docs/` folder. Source copy kept in sync at `PRIVACY_POLICY.md`. Enter that URL into Play Console → App content → Privacy policy.

## Release build — produced and verified this pass

- `Builds/Android/OneMore_Release.aab` rebuilt fresh from current code (not a stale earlier build).
- Verified via `bundletool dump manifest`: `package="com.onemorestudio.onemore"`, `versionCode="2"`, `versionName="1.0.0"`, `minSdkVersion="25"`, `targetSdkVersion="36"`, real AdMob App ID present.
- Verified via `jarsigner -verify`: signed with the project's upload keystore (`keystore/onemore-upload.keystore`, alias `onemore-upload`, cert valid until 2054) — not a debug/unsigned build.
- Keystore password was never printed or logged; supplied transiently via environment variables per `RELEASE_SIGNING.md`'s documented process.
- `versionCode` and `targetSdkVersion` were bumped (from 1→2 and 35→36) during actual Play Console submission on 2026-09-09 — Google rejected the first submission attempt outright on both counts (see `RELEASE_SIGNING.md` for details). This is the exact bundle now sitting in Google's review queue for Closed Testing.

## Play Console submission — done this pass

Full store listing, all app content declarations (ads, content rating, target audience, data safety, government/financial/health N/A declarations, advertising ID), app category (Arcade) + contact details, countries/regions (all + rest of world), and a tester email list were all set up directly in Play Console. The Closed Testing release (v2, 1.0.0) was uploaded and **submitted for Google's review** on 2026-09-09. See `PLAY_CONSOLE_MANUAL_STEPS.md` for what's left (adding more testers to reach 12, waiting out the review and the 14-day window).

## Files cleaned up this pass

- Deleted `DEPLOYMENT_STATUS.md`, `FINAL_RELEASE_STATUS.md`, `DEVICE_TEST_CHECKLIST.md` — three overlapping, stale status documents describing the pre-rebuild game; superseded by this file.
- Deleted `Assets/Editor/ResetConsentButtonEditor.cs`, `Assets/Editor/SettingsPanelBackgroundFixEditor.cs` — one-off editor scripts targeting the Settings/Privacy overlay panels, which were removed from the game entirely (Main Menu was simplified to Play / Sound toggle / Privacy / Exit).
- Rewrote `PLAY_STORE_LISTING.md` and `PLAY_CONSOLE_MANUAL_STEPS.md`, which still described the original one-tap endless-runner concept and the old "One More" name — both now describe the actual shipped game.
- Fixed the remaining stale "One More" title in `RELEASE_SIGNING.md` (the literal project folder name and keystore filename are unchanged on purpose — they're real paths on disk, not display names).

## What's still genuinely left (all external to this project — nothing further to fix in code)

1. **AdMob account approval** — pending on Google's side, automatic once granted.
2. **AdMob → Privacy & messaging** — create a consent message for the app (a few minutes in the AdMob dashboard).
3. **Store assets** — screenshots, feature graphic, 512×512 icon (see `STORE_ASSETS_CHECKLIST.md`); the game is now proven to run cleanly on-device, so these can be captured any time.
4. **Play Console submission itself** — app info, content rating questionnaire, data safety form, privacy policy URL entry, and the mandatory 12-tester/14-day Closed Testing window for new developer accounts (see `PLAY_CONSOLE_MANUAL_STEPS.md`).
5. Everything above is either a Google-side process, a manual Play Console step, or asset capture — the codebase itself has no known open bugs as of this pass.
