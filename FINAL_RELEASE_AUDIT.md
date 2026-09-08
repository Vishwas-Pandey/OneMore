ONE MORE — FINAL RELEASE AUDIT

Read-only verification pass. No code, configuration, or asset changes were made while producing this document. Every claim below was independently re-checked against the actual files/artifact in this pass (bundletool manifest dump, keytool cert inspection, direct scene/ProjectSettings YAML inspection, git ls-files) — not taken on faith from prior session summaries.

GREEN — VERIFIED READY
- Package ID is consistent and correct everywhere it matters: `com.onemorestudio.onemore` in `ProjectSettings/ProjectSettings.asset` (`applicationIdentifier.Android` and `.iPhone`), in `Assets/Plugins/Android/AndroidManifest.xml`, and confirmed in the actual built AAB's manifest (`bundletool dump manifest` → `package="com.onemorestudio.onemore"`).
- Version verified in the actual AAB: `versionCode="1"`, `versionName="1.0.0"` (matches `ProjectSettings.asset`).
- SDK levels verified in the actual AAB: `minSdkVersion="25"`, `targetSdkVersion="35"`, `compileSdkVersion="35"`.
- Architecture verified: only `lib/arm64-v8a` native libraries present in the bundle — no armv7, ARM64-only as configured (`AndroidTargetArchitectures: 2`). IL2CPP scripting backend confirmed.
- Signing verified: extracted the AAB's `META-INF/*.RSA` and ran `keytool -printcert` directly against it — `Owner: CN=One More, OU=OneMoreStudio, O=OneMoreStudio` — this is your own release keystore, not a debug key.
- Permissions verified in the actual AAB manifest: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `WAKE_LOCK`, `AD_ID`, plus `ACCESS_ADSERVICES_AD_ID/ATTRIBUTION/TOPICS` (auto-merged in by the AdMob SDK's own manifest for Privacy Sandbox support — not something added manually, and expected). No contacts/location/camera/microphone/storage permissions present.
- AdMob App ID verified in the actual AAB manifest: `ca-app-pub-3940256099942544~3347511713` — this is Google's public test App ID, confirmed test mode.
- AdMob ad unit IDs verified directly in both scenes' serialized `AdManager` component (not just the .cs defaults): `androidRewardedAdUnitId`/`androidInterstitialAdUnitId`/iOS equivalents are still the literal placeholder strings `ca-app-pub-XXXXXXXXXXXXXXXX/...`, and `useTestAds: 1` in both `MainMenu.unity` and `Gameplay.unity`. Test mode is genuinely active, consistently, everywhere.
- UMP consent verified as a real integration, not a stub: `ConsentManager.cs` imports `GoogleMobileAds.Ump.Api` and calls the actual SDK methods `ConsentInformation.Update(...)` → `ConsentForm.LoadAndShowConsentFormIfRequired(...)` → gates on `ConsentInformation.CanRequestAds()`. Confirmed the `ConsentManager` component is physically attached to the same GameObject as `AdManager` in both `MainMenu.unity` (GameObject 492427230) and `Gameplay.unity` (GameObject 774566674) — it will actually run, not just exist as an orphaned script. `GameManager.InitializeSystems()` confirmed to call consent-gathering before `AdManager.Initialize()`, and to proceed (skipping ad init only) if consent gathering itself fails — gameplay is never blocked.
- iOS ATT verified as a real integration: `ATTManager.cs`'s `DllImport("__Internal")` calls are backed by `Assets/Plugins/iOS/ATTBridge.mm`, which calls Apple's actual `ATTrackingManager` framework API (`requestTrackingAuthorizationWithCompletionHandler`) — not a placeholder.
- Analytics verified: 9 tracking methods exist and cover all 8 originally required events (`game_started`, `game_over`, `restart_pressed`, `score_milestone`, `new_high_score`, `rewarded_ad_offered`, `rewarded_ad_completed`, `interstitial_shown`, plus a bonus `settings_changed`). Confirmed `enableFirebase` defaults `false` and the Firebase send calls are commented out — this is honestly a local-only event queue, not a fake "connected" integration. Confirmed the flush loop is a try/caught fire-and-forget coroutine with no network dependency for core gameplay.
- Game flow verified by direct code inspection: `OnRestartClicked()` unconditionally reloads the Gameplay scene regardless of any ad state. `ShowRewardedAd`'s success callback only fires inside `rewardedAd.Show(reward => ...)` — genuinely gated on the reward callback. The Continue button itself is only ever shown when `CanContinueRun()` is true (ad already loaded), so the "ad not ready" fallback path in `AdManager` is defensive, not something reachable via a visible dead-end button. `ShowInterstitialAd(...)` is called from exactly one place in the entire codebase — `MainMenuController.OnPlayClicked()` — never from anything inside the Gameplay scene, so it structurally cannot appear mid-run.
- Background/resume risk verified: `ProjectSettings/TimeManager.asset` → `Maximum Allowed Timestep: 0.33333334` — Unity's default deltaTime cap is intact, so obstacles (which move via `transform.Translate(... * Time.deltaTime)`) cannot teleport through the player's collider after a long background pause.
- High score persistence verified: `SaveSystem.SaveBestScore`/`LoadBestScore` read/write a local JSON file via `File.ReadAllText`/`WriteAllText` at `Application.persistentDataPath` — synchronous, on-device, no network dependency.
- Build scene order verified: `EditorBuildSettings.asset` → Splash, MainMenu, Gameplay, all enabled, in that order.
- Git safety verified fresh: `git status` clean, local `main` (788d19d) matches `origin/main` (788d19d) exactly. `git ls-files` searched for keystore/secret/env/password filenames — zero matches. `git ls-files` searched for anything under `Library/` or `Builds/` — zero matches. The keystore and its password file exist only on disk, correctly outside version control.
- Privacy policy content verified against actual code: correctly describes local-only save data, AdMob's advertising-identifier/device-info/approximate-location collection, UMP consent, no analytics backend currently transmitting data, no crash reporting, no location/camera/contacts/microphone access. One accuracy gap found — see YELLOW.

YELLOW — MANUAL ACTION REQUIRED
- Create an AdMob account and generate real ad unit IDs: Android App ID, Android interstitial ID, Android rewarded ID (and iOS equivalents if you ship there). `PRODUCTION ADMOB CREDENTIALS REQUIRED`.
- Fill in every `[BRACKETED PLACEHOLDER]` in `PRIVACY_POLICY.md`: your developer/studio name, support email address, developer address (or "not applicable"), and the settings location for "reset ad consent" (see next bullet).
- Wire `ConsentManager.ResetConsentState()` (already exists in code) to an actual button in the Settings panel — it's implemented but not yet reachable by a user. Update `PRIVACY_POLICY.md`'s placeholder for this once done.
- Publish the filled-in privacy policy to a live public HTTPS URL, then enter that URL into Play Console → App content → Privacy policy. A Markdown file in this repo does not satisfy Play's requirement.
- Minor accuracy gap in `PRIVACY_POLICY.md`: it does not explicitly name "App Tracking Transparency" even though `ATTManager`/`ATTBridge.mm` implement it for iOS. Not a Google Play blocker (ATT is an Apple/iOS-only mechanism), but worth adding a sentence if you ever publish to the App Store, since Apple's own review checks that your privacy policy and your actual tracking prompt agree.
- Test on a physical Android device or emulator — `PHYSICAL DEVICE VERIFICATION NOT COMPLETED`. `adb devices` returns an empty list in this environment (checked fresh during this audit); nothing in `DEVICE_TEST_CHECKLIST.md` has actually been run.
- Capture real store screenshots, a feature graphic (1024×500), and a 512×512 store-listing icon from a running build — none exist yet (`STORE_ASSETS_CHECKLIST.md`).
- Complete Play Console's Data Safety questionnaire, content rating questionnaire, target audience selection, and ads declaration — drafted guidance exists in `PLAY_CONSOLE_MANUAL_STEPS.md`, but only you can actually submit these in the console.
- Enroll in Play App Signing at first upload (Play Console prompts for this automatically — cannot be done from this project).
- Run a Closed Testing track in Play Console before Production, and actually work through `DEVICE_TEST_CHECKLIST.md` against that build.
- Back up `keystore/onemore-upload.keystore` and `keystore/keystore.properties` somewhere outside this repo — git will never contain them by design, so no other backup currently exists.

RED — TRUE BLOCKERS
NO CODE/BUILD BLOCKERS FOUND

The `.aab` builds, is correctly signed, and is internally consistent (package ID/version/SDK levels/permissions all verified in the artifact itself). Nothing in the code or Unity configuration currently prevents uploading this AAB to Google Play. The blockers that exist (privacy policy URL, store assets, Play Console questionnaires) are account/console/manual-content tasks, not code or build defects.

ADMOB CONFIGURATION
- Android AdMob App ID: `Assets/Plugins/Android/AndroidManifest.xml` (meta-data `com.google.android.gms.ads.APPLICATION_ID`) — currently Google's public test value.
- Android/iOS interstitial + rewarded ad unit IDs: `Assets/_Project/Scripts/Ads/AdManager.cs`, serialized `AdConfig` fields (`androidInterstitialAdUnitId`, `androidRewardedAdUnitId`, `iosInterstitialAdUnitId`, `iosRewardedAdUnitId`), set per-instance on the `AdManager` component in `MainMenu.unity` and `Gameplay.unity` — currently placeholder strings.
- Dev/prod switch: `AdManager.useTestAds` (bool, currently `true` in both scenes) — when `true`, Google's hardcoded test IDs are used regardless of the `AdConfig` values above; only flip this once real IDs are filled in.
- Required production values you must obtain from your own AdMob account (none invented here): `ADMOB_ANDROID_APP_ID`, `ADMOB_ANDROID_INTERSTITIAL_ID`, `ADMOB_ANDROID_REWARDED_ID`, and if shipping iOS: `ADMOB_IOS_APP_ID`, `ADMOB_IOS_INTERSTITIAL_ID`, `ADMOB_IOS_REWARDED_ID`.

PRIVACY POLICY
Information you must personally provide (none of it invented): your developer/studio legal name, a support contact email, a developer address (or an explicit statement that one isn't required in your jurisdiction), and the in-app location of the "reset ad consent" control once you wire it to a button. Where it must be hosted: a live, public HTTPS URL — Play Console will not accept a link to a file in this GitHub repo or a local document; it needs to resolve as a normal web page (a static site, GitHub Pages, your own domain, etc.).

ANDROID AAB
- Exact path: `Builds/Android/OneMore_Release.aab`
- Package ID: `com.onemorestudio.onemore`
- Version: `1.0.0`
- Version code: `1`
- Target SDK: `35` (min SDK `25`)
- Signing status: signed with your own release keystore (`keystore/onemore-upload.keystore`, alias `onemore-upload`) — confirmed via direct certificate inspection of the bundle, not assumed.

DEVICE TESTING
Nothing has been run on a device yet. `DEVICE_TEST_CHECKLIST.md`'s full 20-step list remains outstanding, in particular: fresh install, rewarded revive actually resuming the run, interstitial frequency in practice, offline mode, app backgrounding/resume, and high-score persistence across a real force-quit — all need a physical device or emulator, neither of which is available in this environment.

GOOGLE PLAY CHECKLIST
1. AdMob → create account, create Android App ID + interstitial + rewarded ad units, note the real IDs.
2. Privacy Policy → fill in all placeholders in `PRIVACY_POLICY.md`, publish it to a public HTTPS URL.
3. Physical Device → install a build (see `DEVICE_TEST_CHECKLIST.md` for how to turn the `.aab` into installable APKs via bundletool), run the full 20-step checklist.
4. Play Console → create the app listing, enter package ID/category, complete Data Safety + content rating + ads declaration + app access + target audience.
5. Closed Testing → upload `OneMore_Release.aab` to a Closed Testing track, add testers, confirm it installs and runs correctly from the Play-distributed build (not just your local copy).
6. Production Access → once Closed Testing passes, only then consider flipping `useTestAds` to `false` with real AdMob IDs and producing a new signed build (bump `AndroidBundleVersionCode` first).
7. Public Release → promote to Production, submit for review.

EXACT NEXT 10 ACTIONS
1. Create your AdMob account and generate the three (or six, with iOS) real ad unit IDs.
2. Fill in every placeholder in `PRIVACY_POLICY.md`.
3. Publish the completed privacy policy to a public HTTPS URL.
4. Get a physical Android device or emulator available, install a build, and run through `DEVICE_TEST_CHECKLIST.md`.
5. Wire `ConsentManager.ResetConsentState()` to a Settings-panel button.
6. Capture real screenshots, a feature graphic, and a 512×512 store icon from the running build.
7. Create the Play Console app listing entry (package ID, category, store text from `PLAY_STORE_LISTING.md`).
8. Complete Play Console's Data Safety, content rating, ads declaration, and app access questionnaires (`PLAY_CONSOLE_MANUAL_STEPS.md`).
9. Upload `OneMore_Release.aab` to a Closed Testing track and validate it there.
10. Once real AdMob IDs exist and device testing passes, flip `useTestAds` to `false`, bump the version code, rebuild, and promote to Production.

READY FOR MANUAL RELEASE STEPS

The code, build configuration, signing, and the AAB artifact itself are all verified correct with no code or build blockers found. What remains is exclusively account-level and content work only you can do — real AdMob credentials, a hosted privacy policy, physical device testing, and Play Console's own submission forms — none of which this project's code can complete on your behalf.
