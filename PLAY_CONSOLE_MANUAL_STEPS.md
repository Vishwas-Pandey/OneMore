# Play Console Manual Steps — One More

Everything below has to be entered directly into Google Play Console by you — none of it can be done from this project or by an automated tool, since it requires your Google Play Developer account, your decisions, and your legal sign-off.

## 1. Basic app info

| Field | Value to enter |
|---|---|
| App name | One More |
| Default language | [YOUR CHOICE — likely English (United States) or English (United Kingdom)] |
| App or game | Game |
| Free or paid | Free (no in-app purchase/paywall exists in the current code — confirm this matches your intent before submitting) |
| Application ID (package name) | `com.onemorestudio.onemore` — this is permanent once you publish; cannot be changed later without publishing as an entirely new app listing. |
| Category | Suggested: **Arcade** (under Games) — final choice is yours. |

## 2. Ads declaration

**Declare: Yes, this app contains ads.** This is factually required — the app integrates Google Mobile Ads SDK and shows rewarded + interstitial ads (currently in test mode; see `DEPLOYMENT_STATUS.md` for what's needed before switching to production ad units).

## 3. Content rating questionnaire

You will fill this out directly in Play Console (IARC questionnaire). Based on what the app actually does — no violence, no user-generated content, no gambling mechanics, no real-money transactions, no chat/social features — expect this to rate as suitable for all ages (Play's "PEGI 3" / "Everyone" equivalent). Answer the actual questionnaire honestly yourself; this is a factual prediction, not something this project can submit on your behalf.

## 4. Target audience and content

- **Target age group**: Since the app is a general-audience arcade game (not specifically designed for or marketed to children) and shows ads via Google's standard AdMob flow (not the Designed for Families program), select the age range that reflects your actual intended audience — do **not** select "primarily appeals to children" unless you specifically intend to join Google Play's Families program, which requires additional compliance work (child-directed ad treatment, no personalized ads to children, stricter data rules) not implemented in this build.
- **Ads**: confirm "Yes" per Section 2.

## 5. Data Safety section

This is the part Play Console scrutinizes most closely, and where guessing is most harmful. Below is based strictly on what's actually in this codebase as of this session — not a template, not an assumption.

### What the App itself collects and transmits
**Nothing.** The app's own code (`SaveSystem.cs`) writes best score / games played / settings toggles to a local file on-device only (`Application.persistentDataPath`). This data is never sent off the device by the app's own code. The app requests no runtime permissions beyond what's needed for ads/haptics (see `DEPLOYMENT_STATUS.md` Section 10 — Vibrate, Wake Lock, Internet, Network State).

### What third-party SDKs in this app may collect
**Google Mobile Ads SDK (AdMob) + User Messaging Platform (UMP)** — the only third-party data-processing SDK actually integrated. Per Google's own published Data Safety disclosures for AdMob, declare (consult Play Console's own AdMob-specific guidance/wizard at declaration time, since Google updates this list — the following reflects what AdMob is documented to collect as of this writing):
- **Device or other IDs** (advertising ID) — collected, used for Advertising, shared with Google.
- **App interactions** (ad impressions/clicks) — collected, used for Advertising and Analytics.
- **Approximate location** (derived from IP, not GPS — this app never requests location permission) — collected, used for Advertising.
- Data collection is **not** required to use the core game (a user can decline ad personalization via the UMP consent flow, or play entirely offline, and the core loop still works — see `DEVICE_TEST_CHECKLIST.md` item 18).

### What is explicitly NOT present (do not over-declare these)
- No Firebase Analytics or any other analytics SDK is actively transmitting data (see `DEPLOYMENT_STATUS.md` Section 7 — the internal `AnalyticsManager` queue is local-only and currently just logs to console; `enableFirebase` defaults false and the Firebase calls are commented out).
- No crash reporting SDK is integrated.
- No account creation, no login, no user-generated content, no in-app purchases, no contacts/location/camera/microphone access.

**When you actually fill out the Data Safety form**: use Play Console's built-in "Read AdMob's data safety guidance" link during the questionnaire — Google keeps this current with what their own SDK does, and it will map more precisely to Play's exact checkbox wording than this document can promise to.

## 6. Privacy Policy URL

**Blocked until you complete this.** `PRIVACY_POLICY.md` in this repo is a complete draft, but Play Console requires a **live public HTTPS URL**, not a file in your repo. Publish the filled-in policy somewhere with a stable URL, then paste that URL into **App content → Privacy policy**.

## 7. App access

Since the app has no login/account system, declare **"All functionality is available without special access"** — there's nothing behind a login wall to explain to reviewers.

## 8. Countries / regions

Your choice — no code in this project restricts geography. If you enable ads in the EEA/UK, the UMP consent flow (already implemented) is what makes that legally viable; nothing further to configure in-app for regional availability itself.

## 9. Closed testing (strongly recommended before Production)

Play Console lets you create a **Closed testing** track first. Recommended before going to Production:
1. Upload `Builds/Android/OneMore_Release.aab` to a Closed testing track.
2. Add yourself (and anyone else) as a tester by email.
3. Actually run through `DEVICE_TEST_CHECKLIST.md` on the installed build from that track.
4. Only promote to Production once that checklist passes.

## 10. Production release

Once Closed testing passes:
1. Create a Production release, upload the same (or a newer, higher-version-code) `.aab`.
2. Write release notes (see below).
3. Submit for review.

### Suggested first release notes
```
First release of One More — a minimalist one-tap arcade game. Tap to jump, survive as long as you can, and try to beat your own best score.
```

## 11. Store listing content
Already drafted in `PLAY_STORE_LISTING.md` (short/full description, feature bullets) and `STORE_ASSETS_CHECKLIST.md` (required image specs — screenshots/feature graphic/icon still need to be captured from an actual running build).

---

## Summary — what's genuinely blocking Production submission right now

1. **Privacy policy public URL** (Section 6) — hard blocker, Play Console will not let you submit without it.
2. **Store screenshots + feature graphic + 512×512 store icon** (see `STORE_ASSETS_CHECKLIST.md`) — need a running build to capture from.
3. **Production AdMob ad unit IDs**, if you want real ad revenue rather than shipping in permanent test-ad mode (see `DEPLOYMENT_STATUS.md` Section 4) — not a submission blocker per se (Play doesn't reject test ads), but you should not launch publicly serving Google's test ads indefinitely.
4. Everything else in this document is data-entry you can complete directly in Play Console once you're ready.
