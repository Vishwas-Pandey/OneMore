# Device Test Checklist — One More

No Android device or emulator was available in this environment (`adb devices` returns an empty list; no simulator equivalent exists for Android in this session). **Runtime testing has not been performed on this build — do not treat anything below as verified until you've actually run through it.**

## How to install the build

`Builds/Android/OneMore_Release.aab` is a Play Store upload artifact, not something you can `adb install` directly. To actually test it on a device before uploading, generate installable APKs from it:

```bash
cd "/Users/vishwaspandey/Desktop/small games /One More"
java -jar "/Applications/Unity/Hub/Editor/6000.3.23f1/PlaybackEngines/AndroidPlayer/Tools/bundletool-all-1.17.2.jar" \
  build-apks --bundle=Builds/Android/OneMore_Release.aab \
  --output=/tmp/onemore.apks \
  --ks=keystore/onemore-upload.keystore \
  --ks-key-alias=onemore-upload \
  --mode=universal
# enter the keystore/key password when prompted (see keystore/keystore.properties)

java -jar "/Applications/Unity/Hub/Editor/6000.3.23f1/PlaybackEngines/AndroidPlayer/Tools/bundletool-all-1.17.2.jar" \
  install-apks --apks=/tmp/onemore.apks
```

Or, simpler for a quick sanity pass: build the existing `Build/Android/Development` variant (already unsigned/debug-signed, just `adb install -r` it) — same scenes and gameplay code, faster iteration, just skip it for the *signing/release-config* verification specifically (use the actual `.aab` for that, per above).

## Test steps

Run through these in order on a real Android device (physical, not just an emulator, for haptics/back-button/backgrounding fidelity):

1. **Fresh installation** — uninstall any prior version first; confirm the install completes and the correct app icon (not the default Unity icon) appears on the home screen/app drawer.
2. **First launch** — app opens without crashing; no permission prompts beyond what's expected (none — this app requests no dangerous runtime permissions).
3. **Splash** — Splash scene displays, then advances to Main Menu without a stall or black-screen hang.
4. **Main Menu** — title/subtitle render and animate; Play/Settings/Privacy buttons are all visible and tappable; best-score readout shows `0000` on a truly fresh install.
5. **Start game** — tapping Play transitions to Gameplay; countdown (3-2-1) displays before control is given to the player.
6. **Score** — tapping makes the player jump; score increments as expected during a run.
7. **Death** — colliding with an obstacle/ground ends the run (death particles/sound/haptic fire).
8. **Game Over** — Game Over panel shows the correct final score; "new best" badge appears only on an actual new best.
9. **Retry** — Restart button always works and starts a fresh run, every time, regardless of what happened with ads.
10. **Rewarded revive** — Continue button appears only when a rewarded ad is actually loaded; watching it to completion resumes the same run (player/obstacles reset, HUD returns) rather than restarting from zero.
11. **Interstitial** — appears only from the Main Menu on "Play", only every Nth game (check `AdManager`'s configured `interstitialFrequency`, currently 3) — confirm it never interrupts an active run.
12. **Settings** — sound/music/haptics toggles visibly change state and actually take effect (mute/unmute, vibration on/off).
13. **Sound** — jump/game-over/button sounds play at expected volumes; toggling sound off actually silences them.
14. **Haptics** — device vibrates on jump (light) and death (strong); toggling haptics off actually stops it.
15. **Background application** — press Home mid-run, wait several seconds, confirm no crash and no watchdog/ANR.
16. **Resume** — reopen the app from the background; confirm the game state is sane (no obstacle teleporting through the player from a large resume delta-time — this was checked in code and should be fine, but worth eyeballing once).
17. **Android back button** — on Main Menu, confirms it closes Settings/Privacy panels first, then exits the app from the root; during Gameplay, confirm it does not crash (expected: currently a no-op mid-run, by design — see `DEPLOYMENT_STATUS.md`).
18. **Offline mode** — enable Airplane Mode, relaunch the app; confirm the core game (Splash → Menu → Play → Score → Game Over → Retry) works fully offline, ads simply fail to load silently (no crash, no blocking dialog), and Continue button correctly does not appear if no rewarded ad loaded.
19. **Restart application** — fully kill and relaunch after a normal play session; confirm no stale state (frozen UI, wrong score) appears.
20. **High score persistence** — after achieving a new best, force-quit and relaunch the app; confirm the best score shown on the Main Menu matches what you just achieved.

## What to report back

For each numbered item above: pass / fail, and for any failure, the exact device model + Android version and what you observed (screenshot or short screen recording helps a lot). This checklist should be run once against the actual signed `OneMore_Release.aab` (or a debug build of the same code, for the earlier gameplay-only steps) before you upload to Play Console's Closed Testing track.
