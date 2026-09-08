# ONE MORE — Setup & Build Guide (Android + iOS)

This repo contains the full Unity source for ONE MORE, wired for both
Android and iOS. This machine doesn't have Unity Editor or Xcode project
tooling installed, so the steps below are what you run locally (on your Mac,
since iOS builds require macOS + Xcode either way) to open the project and
produce real installable builds for both platforms.

## 1. Install prerequisites

1. **Unity Hub** → install **Unity 6000.0 LTS** (or 2022.3 LTS if you prefer
   an older, longer-established LTS — both work with everything in this repo).
   When installing, check these modules:
   - Android Build Support (SDK & NDK Tools, OpenJDK)
   - iOS Build Support
2. **Xcode** (latest from the Mac App Store) — required for the iOS build,
   even though Unity generates the project for you.
3. **Android Studio** is optional (Unity ships its own SDK/NDK via Hub), but
   handy for `adb logcat` while testing on a device.

## 2. Open the project

Open Unity Hub → Add → select this folder (`One More/`). On first open,
Unity will prompt to import **TextMeshPro Essentials** — accept it (the UI
scripts use `TMPro.TextMeshProUGUI`).

## 3. Install the ad SDK

The Google Mobile Ads Unity Plugin is not a Package Manager package — it's a
`.unitypackage`:

1. Download the latest release from
   `https://github.com/googleads/googleads-mobile-unity/releases`
2. `Assets > Import Package > Custom Package...` and import it.
3. This pulls in the **External Dependency Manager for Unity (EDM4U)**,
   which is what resolves CocoaPods for you on iOS (Android needs nothing
   extra; its dependencies resolve via Gradle automatically).
4. After importing, run **Assets > External Dependency Manager > iOS
   Resolver > Install Cocoapods** if prompted — this is the "CocoaPods step"
   iOS needs that Android doesn't.

If you'd rather not set up ads yet, set `AdConfig.enableAds = false` on the
`AdManager` component in the Inspector — every ad call becomes a no-op and
the game runs fine without it.

## 4. Scene setup

Create three scenes under `Assets/_Project/Scenes/`: `Splash`, `MainMenu`,
`Gameplay`, and add them to **File > Build Settings** in that order.

- **MainMenu**: a Canvas with the panels/buttons/text fields
  `MainMenuController.cs` expects (drag each into its Inspector slot), plus
  one `GameManager`, `AudioManager`, `HapticManager`, `AdManager`,
  `AnalyticsManager`, and `ATTManager` object (these use `DontDestroyOnLoad`,
  so they only need to exist once — put them all in MainMenu).
- **Gameplay**: a player GameObject with `PlayerController` + `Rigidbody2D`
  + a trigger `Collider2D`, an `ObstacleSpawner`, a `ScoreManager`, and a
  `GameplayUIController` wired to the countdown/game-over/HUD UI.
- Parent every screen's UI under a `SafeAreaFitter` object so it clears
  notches, the Dynamic Island, and Android cutouts on both platforms.
- Tag obstacles/ground `Obstacle`/`Ground` (matches `PlayerController`'s
  `OnTriggerEnter2D` check).

## 5. Player Settings (per platform)

Under **Edit > Project Settings > Player**:

**Both platforms**
- Company Name / Product Name as you'd like them to appear
- Orientation: Portrait, Portrait Upside Down disabled
- Color Space: Linear

**Android tab**
- Package Name: `com.yourcompany.onemore` (must match `AndroidManifest.xml`
  and your AdMob App ID)
- Minimum API Level: 21, Target API Level: latest
- Scripting Backend: IL2CPP, Target Architectures: ARM64 (and ARMv7 if you
  need older-device reach)

**iOS tab**
- Bundle Identifier: same reverse-DNS style, e.g. `com.yourcompany.onemore`
- Target minimum iOS Version: 13.0+ (14.5+ effectively required anyway for
  the ATT prompt to matter)
- Signing Team ID: your Apple Developer Team ID (needed for Xcode to sign)

## 6. Build

**Android**: menu **Build > Android > Release App Bundle (Play Store)**
(from `Assets/Editor/BuildScript.cs`). Produces
`Builds/Android/OneMore_Release.aab`. Sign it with your release keystore via
Unity's Player Settings > Publishing Settings before uploading to Play
Console.

**iOS**: menu **Build > iOS > Generate Xcode Project**. Unity can only take
iOS this far — it exports an Xcode project, it does not itself produce an
`.ipa`. Then:

1. Open `Builds/iOS/Unity-iPhone.xcworkspace` in Xcode (not the `.xcodeproj`).
2. Select the `Unity-iPhone` target → Signing & Capabilities → choose your
   Team.
3. Plug in a device or pick a Simulator, then **Product > Run** to test, or
   **Product > Archive** → **Distribute App** to upload to App Store Connect
   for TestFlight/release.

`Assets/Editor/IOSBuildPostProcessor.cs` runs automatically during that
export step and adds the `NSUserTrackingUsageDescription` string,
`SKAdNetworkItems`, and links `AppTrackingTransparency.framework` /
`AdSupport.framework` for you — you don't need to touch Info.plist by hand.

## 7. What's shared vs. platform-specific

- **Shared (~95% of the code)**: `GameManager`, `PlayerController`,
  `ObstacleSpawner`, `ScoreManager`, `SaveSystem`, `ObjectPool`, all UI
  controllers, `AnalyticsManager`, `SafeAreaFitter` — identical on both OSes.
- **Android-only path**: `HapticManager`'s `AndroidJavaObject` Vibrator call;
  `AndroidManifest.xml`.
- **iOS-only path**: `HapticsBridge.mm` / `ATTBridge.mm` native plugins,
  `ATTManager.cs`, `IOSBuildPostProcessor.cs`.
- **Ads**: `AdManager.cs` is shared, but resolves separate test/production ad
  unit IDs per platform (Google issues different IDs for Android vs. iOS).

## 8. Before you submit to either store

- Replace every placeholder AdMob ID (`ca-app-pub-XXXX...`) in `AdManager`'s
  Inspector fields and in `AndroidManifest.xml` with your real IDs, and flip
  `useTestAds` to `false`.
- Write your actual Privacy Policy and point `privacyPanel` at it (both
  stores require one; Apple additionally needs the "App Privacy" nutrition
  label filled in on App Store Connect).
- Fill in real store listing assets — see the "Deployment Guide" section of
  the original spec doc for the exact icon/screenshot sizes and content
  rating steps for both Google Play and the Apple App Store.
