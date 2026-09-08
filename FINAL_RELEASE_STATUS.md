ONE MORE RELEASE STATUS

GREEN — READY
- Android package ID consistent everywhere: com.onemorestudio.onemore (manifest placeholder fixed)
- Version set: 1.0.0, version code 1, target SDK 35, min SDK 25, ARM64 + IL2CPP
- Real app icon generated and wired into PlayerSettings for Android + iOS (both were empty before)
- Release upload keystore created; PlayerSettings wired to it; passwords kept out of git and out of chat
- Signed, verified Builds/Android/OneMore_Release.aab produced (package id, version, SDK levels, and signing certificate all confirmed via bundletool + keytool, not just assumed)
- AdMob integration audited: test IDs confirmed still active everywhere (safe), rewarded/interstitial lifecycle confirmed correct (revive only on real reward, ads never block retry, interstitial never fires mid-gameplay), one real drift bug fixed (interstitial frequency was hardcoded separately from AdManager's own config)
- Google UMP consent flow implemented and wired in (previously entirely missing) — gates ad initialization, never blocks gameplay if it fails
- Analytics event coverage confirmed complete (all 8 required events already existed)
- Gameplay release audit complete: high-score persistence, retry-always-works, ads-can't-trap-player, and a background/resume delta-time spike risk all specifically checked — no redesign, no regressions introduced
- Android permissions confirmed minimal (no contacts/location/camera/mic/storage)
- Build scene order confirmed correct (Splash, MainMenu, Gameplay)
- Git repository initialized, scoped strictly to the One More project (home-directory repo never touched), proper Unity .gitignore in place, baseline + changes committed and pushed to https://github.com/Vishwas-Pandey/OneMore
- Security scan of all tracked source: no secrets found; the one credential this session created (keystore password) is gitignored and was never printed

YELLOW — NEEDS MANUAL ACTION (things only you can do)
- Create/confirm your AdMob account and generate real ad unit IDs (Android + iOS app ID, interstitial ID, rewarded ID) — until then the app ships correctly in Google's test-ad mode, which is safe but earns no revenue
- Publish PRIVACY_POLICY.md somewhere with a public HTTPS URL, then paste that URL into Play Console (App content → Privacy policy) — this is a hard submission blocker, not optional
- Enroll in Play App Signing at first upload (Play Console prompts for this automatically)
- Run through DEVICE_TEST_CHECKLIST.md on an actual device/emulator — nothing in this build has been runtime-verified beyond compiling and producing a valid signed artifact, because no device or emulator was available in this environment
- Capture real screenshots + feature graphic + 512×512 store icon from a running build (see STORE_ASSETS_CHECKLIST.md) — none exist yet
- Fill out Play Console's Data Safety questionnaire, content rating questionnaire, and category/audience selections (see PLAY_CONSOLE_MANUAL_STEPS.md for exactly what to enter and why)
- Wire a "reset ad consent" option into the Settings panel (ConsentManager.ResetConsentState() already exists, just isn't hooked to a button yet) — not a launch blocker, but expected by store review norms
- Decide whether to keep the AAB filename bumping AndroidBundleVersionCode by hand before every future release (currently manual, documented in RELEASE_SIGNING.md)
- Back up keystore/onemore-upload.keystore and keystore/keystore.properties somewhere outside this repo (a password manager attachment, encrypted drive) — git will never contain them, by design, so this is the only real backup that will exist

RED — BLOCKERS (genuinely prevent Play Store upload right now)
- No public privacy policy URL exists yet — Play Console will refuse submission without one
- No AdMob production credentials exist — not a blocker to build/upload, but a blocker to real ad revenue; do not flip useTestAds to false until you have them
- No store listing screenshots/feature graphic/store icon exist yet — Play Console requires these to publish (though not to build the AAB itself)

BUILD ARTIFACTS
- Development APK (pre-existing, unsigned/debug): Builds/Android/OneMore_Development.apk
- Production App Bundle (this session, signed, verified): Builds/Android/OneMore_Release.aab (29,033,549 bytes)
- iOS Xcode project (pre-existing, untouched this session — Android was the priority per instructions): Builds/iOS/

PRODUCTION CONFIGURATION (keys/names only — no secret values)
- Package ID: com.onemorestudio.onemore
- Version name / code: 1.0.0 / 1
- Min SDK / Target SDK: 25 / 35
- Keystore path: keystore/onemore-upload.keystore (gitignored)
- Keystore alias: onemore-upload
- Signing password source: keystore/keystore.properties (gitignored, local only) — read into ONEMORE_KEYSTORE_PASS / ONEMORE_KEY_ALIAS_PASS environment variables at build time by Assets/Editor/BuildScript.cs
- AdMob mode: test IDs (useTestAds = true in AdManager.cs) — production values needed: ADMOB_ANDROID_APP_ID, ADMOB_ANDROID_INTERSTITIAL_ID, ADMOB_ANDROID_REWARDED_ID (iOS equivalents documented in DEPLOYMENT_STATUS.md if you ship there)
- Consent: Google UMP, implemented in Assets/_Project/Scripts/Ads/ConsentManager.cs

EXACT PRODUCTION BUILD COMMAND
    cd "/Users/vishwaspandey/Desktop/small games /One More"
    export ONEMORE_KEYSTORE_PASS="$(grep '^storePassword=' keystore/keystore.properties | cut -d= -f2-)"
    export ONEMORE_KEY_ALIAS_PASS="$(grep '^keyPassword=' keystore/keystore.properties | cut -d= -f2-)"
    /Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
      -batchmode -quit -projectPath "$(pwd)" \
      -executeMethod BuildScript.BuildAndroidAppBundle \
      -logFile /tmp/build.log
    unset ONEMORE_KEYSTORE_PASS ONEMORE_KEY_ALIAS_PASS

EXACT LOCATION OF PRODUCTION AAB
    /Users/vishwaspandey/Desktop/small games /One More/Builds/Android/OneMore_Release.aab

EXACT BLOCKERS BEFORE GOOGLE PLAY UPLOAD
1. Publish PRIVACY_POLICY.md content to a public HTTPS URL and enter it in Play Console
2. Capture and upload store screenshots, feature graphic, and 512×512 store icon
3. Complete Play Console's Data Safety + content rating + app access questionnaires (see PLAY_CONSOLE_MANUAL_STEPS.md)
4. (Recommended, not a hard blocker) Run DEVICE_TEST_CHECKLIST.md on a real device before promoting past Closed Testing
5. (Recommended, not a hard blocker) Obtain real AdMob production IDs before relying on ad revenue — test ads work fine for submission itself
