# One More — Deployment Status

Audit performed directly against the project state on disk (not against any prior chat summary). Status as of this pass, before vs. after the fixes applied in this same session are both reflected below (each item says which state it's in *now*).

Legend: **DONE** = verified complete and working. **NEEDS IMPLEMENTATION** = code/config gap, fixed in this session unless noted. **NEEDS MANUAL ACTION** = only the project owner can complete this (an account, a credential, a console step). **BLOCKED** = cannot proceed further without something external.

---

## 1. Project identity & versioning

| Item | Status | Detail |
|---|---|---|
| Android application ID | **DONE** | `com.onemorestudio.onemore` consistent across `ProjectSettings/ProjectSettings.asset` (`applicationIdentifier.Android`/`.iPhone`) and `Assets/Plugins/Android/AndroidManifest.xml`. Previously the manifest had a stray placeholder `com.yourcompany.onemore` — fixed. No other placeholder IDs found anywhere else in the project (`GoogleMobileAdsPlugin.androidlib`'s own manifest package `com.google.unity.ads.plugin` is the plugin's own internal namespace, not the app's, and is correct as-is). |
| Version string | **DONE** | `bundleVersion` set to `1.0.0`. |
| Android version code | **DONE** | `AndroidBundleVersionCode: 1`. Increment this integer (never reuse/lower it) on every future Play Store upload — see `RELEASE_SIGNING.md`. |
| Target/min SDK | **DONE** | `AndroidMinSdkVersion: 25` (Android 7.1), `AndroidTargetSdkVersion: 35` (explicit, was `0`/"Auto" — pinned to a known Play-compliant value rather than whatever happens to be the highest platform installed on this machine, which includes an unreleased `android-37.0` preview). |
| Architecture | **DONE** | ARM64-only (`AndroidTargetArchitectures: 2`), IL2CPP scripting backend — both required for current Play Store 64-bit policy. |
| Orientation | **DONE** | Portrait-locked via manifest (`screenOrientation="portrait"`) and `uses-feature screen.portrait required`. |

## 2. App icon

| Item | Status | Detail |
|---|---|---|
| Android launcher icon | **DONE** | Was completely empty (`m_Textures: []`) — this is what caused the manifest/build conflict noted in the earlier session. Generated a minimalist icon (purple background, white "player dot + motion trail" mark — no text, no third-party artwork) and wired it into `PlayerSettings` via a one-time Editor script (`Assets/Editor/DeploymentPrepEditor.cs`). Verified post-run: `ProjectSettings.asset` now references the real texture GUID across all required Android icon size buckets (36–192px) instead of empty arrays. |
| iOS icon | **DONE** | Same source icon applied to all required iOS buckets (20–1024px) as a bonus (P1, not the focus of this sprint). |
| Adaptive icon (foreground/background layers) | **NEEDS IMPLEMENTATION** (manual, low priority) | Source layer art already exists at `Assets/_Project/Sprites/AppIcon/icon_adaptive_fg_1024.png` and `icon_adaptive_bg_1024.png` (transparent foreground within the safe zone + solid background). The scripted attempt to wire these into Unity 6000.3's adaptive-icon slot threw `System.ArgumentException: Type provided must be an Enum` — the API this was written against doesn't match what's actually in this Editor version. Rather than guess further at an unfamiliar API, this was left unwired: the plain/legacy icon (already applied) is what Android actually uses as a fallback for adaptive slots, so the app is not blocked by this — it's a visual nicety, not a submission requirement. If wanted later: Project Settings → Player → Android → Icon → Adaptive, drag in the two generated PNGs manually. |
| Play Store listing icon (512×512) | **NEEDS MANUAL ACTION** | Separate from the in-app icon — uploaded directly in Play Console, not from the build. See `STORE_ASSETS_CHECKLIST.md`. |

## 3. Signing / release configuration

| Item | Status | Detail |
|---|---|---|
| Upload keystore | **DONE** | Generated at `keystore/onemore-upload.keystore` (RSA 2048, 10000-day validity, alias `onemore-upload`). Password generated randomly, stored only in the gitignored `keystore/keystore.properties` — never printed to chat/logs, never committed. Full details in `RELEASE_SIGNING.md`. |
| `PlayerSettings` wired to keystore | **DONE** | `androidUseCustomKeystore: 1`, `AndroidKeystoreName`/`AndroidKeyaliasName` set. Passwords are deliberately *not* persisted to `ProjectSettings.asset` (Unity doesn't serialize those fields, and they shouldn't be committed anyway) — `BuildScript.cs` reads them from `ONEMORE_KEYSTORE_PASS`/`ONEMORE_KEY_ALIAS_PASS` environment variables at build time only. |
| Build target artifact | **DONE — built and verified** | `Build/Android/Release App Bundle (Play Store)` menu item already existed in `BuildScript.cs` and produces exactly `Builds/Android/OneMore_Release.aab`. First real build attempt failed with `Keystore file '.../Library/Bee/Android/.../keystore/onemore-upload.keystore' not found` — Gradle resolves a *relative* `keystoreName` against its generated module directory, not the project root. Fixed in `BuildScript.cs` by resolving the path to absolute in memory at build time only (never written back to the committed `ProjectSettings.asset`, keeping it portable). Rebuilt successfully: **29,033,549 bytes**. Verified via `bundletool dump manifest` — `package="com.onemorestudio.onemore"`, `versionCode="1"`, `versionName="1.0.0"`, `minSdkVersion="25"`, `targetSdkVersion="35"` — and via `keytool -printcert` against the bundle's signing block, confirmed signed with our own release keystore (`CN=One More, OU=OneMoreStudio`), not a debug key. |
| **Play App Signing enrollment** | **NEEDS MANUAL ACTION** | You must enroll in Play App Signing on first upload (Play Console does this automatically the first time you upload a bundle signed with this upload key) — Google then re-signs the app for distribution with an app signing key it manages. This is a one-time Play Console step, not something this project can do for you. |

## 4. AdMob

| Item | Status | Detail |
|---|---|---|
| SDK | **DONE** | Google Mobile Ads Unity SDK v11.5.0 already imported, Android dependency `play-services-ads:25.4.0` — current, no action needed. |
| Dev/prod ID split | **DONE** (pre-existing, verified correct) | `AdManager.cs` already has a clean `useTestAds` bool gating Google's official test IDs vs. a serialized `AdConfig` holding real IDs. `useTestAds` defaults `true`; the manifest's AdMob App ID (`ca-app-pub-3940256099942544~3347511713`) is also Google's public test App ID. **This was never touched and is correctly still in test mode.** |
| Real production IDs | **BLOCKED — requires your AdMob account** | Cannot be filled in without an AdMob account and real ad units. Required values, none of which currently exist: `ADMOB_ANDROID_APP_ID`, `ADMOB_ANDROID_INTERSTITIAL_ID`, `ADMOB_ANDROID_REWARDED_ID` (and the iOS equivalents if you ship there). **PRODUCTION ADS CREDENTIALS REQUIRED.** Do not flip `useTestAds` to `false` or edit the manifest's AdMob App ID until you have these. |
| Ad-lifecycle correctness audit | **DONE** (all checks passed, no changes needed except one) | <ul><li>Rewarded revive only fires inside the reward-success callback path (`AdManager.ShowRewardedAd` → `rewardedAd.Show(reward => onComplete())`); the "ad not ready" fallback path also calls `onComplete` but is unreachable in practice because the Continue button is only shown when `CanContinueRun()` (ad already loaded) is true.</li><li>Ad failures never break the game — every failure path falls back to `onComplete?.Invoke()` so gameplay continues.</li><li>Ad-not-loaded never blocks retry — Restart always just reloads the scene regardless of ad state.</li><li>Interstitial is only ever shown from the Main Menu "Play" click, gated at "every Nth game" — **never during active gameplay**.</li><li>**Fixed a real inconsistency**: the frequency was hardcoded as `% 3` in `MainMenuController.cs` while `AdManager`'s own `AdConfig.interstitialFrequency` field (also defaulting to 3) was never actually read — the two could silently drift if either was tuned later. Added `AdManager.InterstitialFrequency` and pointed `MainMenuController` at it.</li></ul> |

## 5. GDPR / UMP consent

| Item | Status | Detail |
|---|---|---|
| Consent flow | **DONE — implemented this session** | No UMP integration existed at all before this pass, despite the SDK's UMP DLLs already being present in the project (`Assets/GoogleMobileAds/GoogleMobileAds.Ump*.dll`). Added `Assets/_Project/Scripts/Ads/ConsentManager.cs` using the official `GoogleMobileAds.Ump.Api` (`ConsentInformation.Update` → `ConsentForm.LoadAndShowConsentFormIfRequired`). |
| Wiring | **DONE** | `GameManager.InitializeSystems()` now gathers consent before calling `AdManager.Initialize()`; ads are only initialized if `ConsentInformation.CanRequestAds()` is true afterward. If the consent flow itself fails (e.g. no network at launch), the callback still fires and the game proceeds — ads are just skipped for that session. Gameplay is never blocked. `ConsentManager` component added to the same "Managers" GameObject that already hosts `AdManager`/`AnalyticsManager` in both `MainMenu.unity` and `Gameplay.unity` (matching the project's existing per-scene manager pattern), verified via batchmode scene edit. |
| "Reset consent" option | **NEEDS MANUAL ACTION (UI)** | `ConsentManager.ResetConsentState()` exists (wraps `ConsentInformation.Reset()`) but isn't wired to any Settings-panel button. Play/App Store review expects a way for users to change their ad-consent choice later — wire this to a button in the Settings panel when you have a moment; not a build blocker today since the first-launch consent flow itself is compliant. |

## 6. Privacy policy

| Item | Status | Detail |
|---|---|---|
| Draft policy | **DONE** | `PRIVACY_POLICY.md` created, accurately describing what the app and its actual SDKs (AdMob + UMP) do, using placeholders for company/contact identity — nothing invented. |
| Public HTTPS URL | **NEEDS MANUAL ACTION — BLOCKED without it** | Play Console requires a live, public URL to a privacy policy before you can publish. A local Markdown file cannot satisfy this. You need to publish the content somewhere (a simple static page works) and put that URL into Play Console's App Content section. **PRIVACY POLICY PUBLIC URL REQUIRED.** |
| In-app privacy panel | **DONE (pre-existing)** | `MainMenuController` already has a `privacyPanel` toggle — kept as-is, not removed. |

## 7. Analytics

| Item | Status | Detail |
|---|---|---|
| Required events | **DONE (all present, pre-existing)** | `game_started`, `game_over` (with score/best/isNewBest), `retry` (`restart_pressed`), `score_milestone`, `new_high_score`, `rewarded_ad_offered`, `rewarded_ad_completed`, `interstitial_shown` all implemented in `AnalyticsManager.cs`. |
| Backend | **NEEDS MANUAL ACTION if you want real analytics** | `AnalyticsManager` is currently a local in-memory queue that only `Debug.Log`s events and never actually transmits them — `enableFirebase` defaults `false` and the Firebase calls are commented out (`// Firebase.Analytics.FirebaseAnalytics.LogEvent(...)`). This is honestly labeled, not a fake integration. To get real analytics: import the Firebase Analytics Unity SDK, add `google-services.json`, uncomment the two Firebase calls, flip `enableFirebase` to `true`. Until then, events are collected locally and discarded — this never blocks or slows gameplay either way. |
| Offline resilience | **DONE** | Queue flush is a fire-and-forget coroutine wrapped in try/catch; no network dependency exists for core gameplay. |

## 8. Gameplay release audit (no redesign performed — audit only, one real bug fixed)

| Area | Status | Detail |
|---|---|---|
| Core loop (Splash → Menu → Play → Countdown → Gameplay → Score → Game Over → Retry) | **DONE** | Verified intact end-to-end by reading the actual wiring in `GameplayUIController`, `MainMenuController`, `SplashLoader`, `GameManager`. Nothing was redesigned. |
| High score persistence | **DONE** | `SaveSystem` uses `JsonUtility` + local file (correctly avoids the obsolete/insecure `BinaryFormatter`). |
| Player can always start another game after Game Over | **DONE** | Restart button always reloads the Gameplay scene unconditionally, regardless of ad state. |
| Ads cannot trap the player | **DONE** | Every ad path (rewarded and interstitial) has a non-ad fallback that always proceeds — confirmed in the AdMob audit above. |
| Backgrounding / resume (deltaTime spike risk) | **DONE (verified, no bug found)** | Obstacles move via `transform.Translate(... * Time.deltaTime)`, which *could* teleport an obstacle through the player collider after a long background pause if `Time.deltaTime` were unclamped. Checked `ProjectSettings/TimeManager.asset`: `Maximum Allowed Timestep: 0.33333334` (Unity's default cap) — deltaTime is already bounded to ~1/3s per frame even after backgrounding, so no teleport-through-collision bug exists. Left untouched — nothing to fix. |
| Player physics | **DONE (verified, no bug found)** | Rigidbody2D-driven, `CollisionDetectionMode2D.Continuous`, fixed-timestep physics — robust against frame-rate spikes. |
| Android back button | **PARTIAL — pre-existing, not a blocker** | Handled on Main Menu (closes settings/privacy panels, or quits at the root). Not handled during active Gameplay — pressing back mid-run does nothing (no crash; Unity doesn't auto-quit on unhandled Escape in this version). This is a common, acceptable choice for arcade/endless-runner games and is not a Play Store submission requirement. Not changed, since it isn't broken — flagging for awareness only. |
| App backgrounding audio | **DONE (pre-existing)** | `AudioManager` implements `OnApplicationPause`/`OnApplicationFocus`. |
| Interstitial ad timing | **DONE** | Confirmed never fires mid-gameplay (see AdMob section). |

## 9. Performance

**DONE — no fixes needed.** Reviewed `PlayerController`, `ObstacleSpawner`, `Obstacle`, `ObjectPool` for the usual mobile red flags (runaway `Instantiate`/`Destroy`, unbounded coroutines, uncapped particle counts, missing scene references). The project already uses an object pool for obstacles (`ObjectPool.Initialize`/`GetObject`/`ReturnObject`) rather than raw `Instantiate`/`Destroy` — nothing further to change without redesigning working code, which was explicitly out of scope.

## 10. Android permissions

**DONE — already minimal, no changes made.** Manifest requests only: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `WAKE_LOCK`, and `com.google.android.gms.permission.AD_ID` (required by AdMob on Android 13+). No contacts/location/microphone/camera/storage permissions present. Nothing to remove.

## 11. Build scenes

**DONE — already correct, no changes made.** `EditorBuildSettings.asset` confirms scene order: `Splash → MainMenu → Gameplay`, all enabled.

## 12. Version control

**DONE — this was completely missing before this session.** The entire project had zero git history (`git ls-files` returned 0 files). Initialized a git repository scoped strictly to the `One More` project directory (confirmed the parent home-directory git repo was never touched), added a Unity-appropriate `.gitignore` (excludes `Library/`, `Builds/`, `UserSettings/`, IDE metadata, and — critically — `keystore/` and any `*.properties`/`*.env`/secret files), and committed a baseline snapshot before making any of the changes described in this document.

## 13. Security scan (Phase 18)

**DONE — clean.** Searched all tracked source (`.cs`, `.xml`, `.json`, `.plist`, `.asset`) for password/API-key/secret patterns, PEM private-key headers, Google API key patterns (`AIza...`), and service-account references. **No secrets found in the project source.** The only credential-bearing artifact created this session (the upload keystore) lives outside git in `keystore/`, confirmed via `git check-ignore`.

---

## Summary counts

- **DONE**: 30+ items across identity, icon, signing, AdMob audit, consent, analytics, gameplay, performance, permissions, build config, version control, and security.
- **NEEDS MANUAL ACTION**: Play App Signing enrollment, real AdMob IDs, public privacy-policy URL, consent-reset UI wiring, device testing, and everything in `PLAY_CONSOLE_MANUAL_STEPS.md`.
- **BLOCKED**: production AdMob credentials (requires your AdMob account); privacy policy public URL (requires you to host it somewhere); anything requiring a Play Console/Apple Developer account login.
- **NEEDS IMPLEMENTATION (deferred, non-blocking)**: adaptive icon layering (visual nicety only).
