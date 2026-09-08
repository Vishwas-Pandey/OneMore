# Release Signing — Flying Bird (Android)

## What exists

- **Keystore file**: `keystore/onemore-upload.keystore` (project-relative, inside the `One More` folder).
- **Alias**: `onemore-upload`
- **Key type**: RSA 2048-bit, 10,000-day validity (~27 years — standard practice so the upload key itself never expires mid-lifecycle of the app).
- **Passwords**: generated randomly at creation time, stored **only** in `keystore/keystore.properties` (same directory). The store password and key password are identical (one value covers both).

## Where the passwords actually are

Open `keystore/keystore.properties` yourself to see them — this document deliberately does not repeat the value anywhere (in chat or in any file that gets committed). The properties file has permissions locked to your user only (`chmod 600`).

## Why none of this is in git

`.gitignore` excludes the entire `keystore/` directory (and, as a second layer of protection, any file named `*.keystore`, `*.jks`, or `keystore.properties` anywhere in the repo). **This is intentional and required** — if the upload keystore or its password is ever committed and pushed to a public or shared repository, anyone with access could sign malicious updates that Play would accept as coming from you. Losing it is bad (see below); leaking it is worse.

Confirmed via `git check-ignore -v` that both the keystore file and the properties file are correctly ignored.

## How PlayerSettings is wired

`ProjectSettings/ProjectSettings.asset` has:
```
androidUseCustomKeystore: 1
AndroidKeystoreName: keystore/onemore-upload.keystore
AndroidKeyaliasName: onemore-upload
```
These three values are safe to commit — a path and an alias name are not secrets. Unity does **not** persist `keystorePass`/`keyaliasPass` into this file (by design), so the passwords never end up in version control even by accident.

## How the passwords reach the build

`Assets/Editor/BuildScript.cs` reads two environment variables immediately before building — `ONEMORE_KEYSTORE_PASS` and `ONEMORE_KEY_ALIAS_PASS` — and assigns them to `PlayerSettings.Android.keystorePass`/`keyaliasPass` in memory, for that build only. If either is missing, the build logs a clear warning and Gradle will fail signing with its own explicit error (never a silent/broken artifact).

**One extra wrinkle discovered while building**: Unity/Gradle resolves a *relative* `keystoreName` against its generated Gradle module directory (`Library/Bee/Android/...`), not the project root — so the same relative path that's correct to read in `ProjectSettings.asset` will fail signing if handed to Gradle as-is. `BuildScript.cs` compensates by resolving the path to an absolute one in memory (`Path.GetFullPath`) right before the build, without ever writing that absolute path back to the committed asset — so the repo stays portable across machines.

To run a release build yourself from a terminal (this is exactly what was used to produce the shipped `.aab`):

```bash
cd "/Users/vishwaspandey/Desktop/small games /One More"
export ONEMORE_KEYSTORE_PASS="$(grep '^storePassword=' keystore/keystore.properties | cut -d= -f2-)"
export ONEMORE_KEY_ALIAS_PASS="$(grep '^keyPassword=' keystore/keystore.properties | cut -d= -f2-)"
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod BuildScript.BuildAndroidAppBundle \
  -logFile /tmp/build.log
unset ONEMORE_KEYSTORE_PASS ONEMORE_KEY_ALIAS_PASS
```

Or, from the Unity Editor GUI: **Build → Android → Release App Bundle (Play Store)** menu item — but note the GUI path will prompt for the keystore password interactively since the environment variables won't be set; that's expected and fine for a manual build.

## Version code bumping

`AndroidBundleVersionCode` in `ProjectSettings/ProjectSettings.asset` is currently `1`. **Every future Play Store upload requires a strictly higher integer than any previously-uploaded version** — Play Console rejects a re-upload with the same or lower code. Bump this by hand (or via a small build-time increment step, not currently automated) before each new release build. `PlayerSettings.bundleVersion` (`1.0.0`) is the human-readable version string and has no such monotonic requirement, but should still make sense alongside the version code (e.g. `1.0.1` → code `2`).

## Play App Signing (manual, one-time, in Play Console)

The keystore described here is your **upload key**, not the key Google actually uses to sign the APK/AAB delivered to users. On your **first** upload to Play Console, enroll in **Play App Signing** (Play Console will offer this automatically) — Google then holds the real app signing key and re-signs what you upload with it. This is standard, recommended, and irreversible-by-design (Google explicitly wants you to never lose the distribution key) — you cannot do this step from this project, it happens in the Play Console UI at upload time.

## If you ever lose this keystore

Because Play App Signing (above) holds the actual distribution key, losing your local *upload* key is recoverable but requires manual intervention:
1. Contact Google Play support (or use the in-console "request upload key reset" flow, where available) to register a new upload key.
2. Generate a new keystore the same way this one was created (see the `keytool -genkeypair` invocation used originally — same shape, new random password).
3. Update `AndroidKeystoreName`/`AndroidKeyaliasName` in `ProjectSettings.asset` and the new `keystore/keystore.properties` to match.

**Do not delete `keystore/onemore-upload.keystore` or `keystore/keystore.properties` without a verified backup elsewhere** (e.g. a password manager attachment, an encrypted external drive) — this project's `.gitignore` guarantees git will never be that backup.
