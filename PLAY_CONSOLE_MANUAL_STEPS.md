# Play Console Manual Steps — Flying Bird

Everything below has to be entered directly into Google Play Console by you — none of it can be done from this project or by an automated tool, since it requires your Google Play Developer account, your decisions, and your legal sign-off.

## 1. Basic app info

| Field | Value to enter |
|---|---|
| App name | Flying Bird |
| Developer account | Virevia |
| Default language | [YOUR CHOICE — likely English (United States) or English (United Kingdom)] |
| App or game | Game |
| Free or paid | Free (no in-app purchase/paywall exists in the current code — confirm this matches your intent before submitting) |
| Application ID (package name) | `com.onemorestudio.onemore` — this is permanent once you publish; cannot be changed later without publishing as an entirely new app listing. |
| Category | Suggested: **Arcade** (under Games) — final choice is yours. |

## 2. Ads declaration

**Declare: Yes, this app contains ads.** The app integrates Google Mobile Ads SDK and shows banner, interstitial, and rewarded ads with real (production) AdMob ad unit IDs — not test IDs. As of this writing the AdMob account itself is still pending Google's approval, so live ads are serving Google's own placeholder "Test Ad" content in the meantime; this resolves automatically once the account is approved and requires no further app changes.

## 3. Content rating questionnaire

You will fill this out directly in Play Console (IARC questionnaire). Based on what the app actually does — no violence, no user-generated content, no gambling mechanics, no real-money transactions, no chat/social features — expect this to rate as suitable for all ages (Play's "PEGI 3" / "Everyone" equivalent). Answer the actual questionnaire honestly yourself; this is a factual prediction, not something this project can submit on your behalf.

## 4. Target audience and content

- **Target age group**: Since the app is a general-audience arcade game (not specifically designed for or marketed to children) and shows ads via Google's standard AdMob flow (not the Designed for Families program), select the age range that reflects your actual intended audience — do **not** select "primarily appeals to children" unless you specifically intend to join Google Play's Families program, which requires additional compliance work (child-directed ad treatment, no personalized ads to children, stricter data rules) not implemented in this build.
- **Ads**: confirm "Yes" per Section 2.

## 5. Data Safety section

This is the part Play Console scrutinizes most closely, and where guessing is most harmful. Below is based strictly on what's actually in this codebase.

### What the App itself collects and transmits
**Nothing.** The app's own code (`SaveSystem.cs`) writes best score / games played / the sound on-off toggle to a local file on-device only (`Application.persistentDataPath`). This data is never sent off the device by the app's own code. The app requests no runtime permissions beyond what's needed for ads/haptics (Vibrate, Wake Lock, Internet, Network State, Ad ID).

### What third-party SDKs in this app may collect
**Google Mobile Ads SDK (AdMob) + User Messaging Platform (UMP)** — the only third-party data-processing SDK actually integrated. Per Google's own published Data Safety disclosures for AdMob, declare (consult Play Console's own AdMob-specific guidance/wizard at declaration time, since Google updates this list):
- **Device or other IDs** (advertising ID) — collected, used for Advertising, shared with Google.
- **App interactions** (ad impressions/clicks) — collected, used for Advertising and Analytics.
- **Approximate location** (derived from IP, not GPS — this app never requests location permission) — collected, used for Advertising.
- Data collection is **not** required to use the core game (a user can decline ad personalization via the UMP consent flow, and the core loop still works).

### What is explicitly NOT present (do not over-declare these)
- No Firebase Analytics or any other analytics SDK is actively transmitting data (the internal `AnalyticsManager` queue is local-only and currently just logs to console; `enableFirebase` defaults false and the Firebase calls are commented out).
- No crash reporting SDK is integrated.
- No account creation, no login, no user-generated content, no in-app purchases, no contacts/location/camera/microphone access.

**When you actually fill out the Data Safety form**: use Play Console's built-in "Read AdMob's data safety guidance" link during the questionnaire — Google keeps this current with what their own SDK does, and it will map more precisely to Play's exact checkbox wording than this document can promise to.

## 6. Privacy Policy URL

**Done.** Live at: **https://vishwas-pandey.github.io/OneMore/privacy-policy.html** — enter this exact URL into **App content → Privacy policy**. (Source markdown also kept at `PRIVACY_POLICY.md` in this repo; the hosted HTML page at `docs/privacy-policy.html` is the one that's actually live and should be kept in sync if the policy text changes.)

## 7. App access

Since the app has no login/account system, declare **"All functionality is available without special access"** — there's nothing behind a login wall to explain to reviewers.

## 8. Countries / regions

Your choice — no code in this project restricts geography. If you enable ads in the EEA/UK, the UMP consent flow (already implemented) is what makes that legally viable; nothing further to configure in-app for regional availability itself.

## 9. Closed testing (required for new developer accounts)

Google requires new/personal Play Console developer accounts to run a **Closed testing** track with at least 12 opted-in testers for 14 continuous days before Production is unlocked. Sequence:
1. Upload `Builds/Android/OneMore_Release.aab` to a Closed testing track.
2. Add at least 12 testers by email; they must actually opt in and install.
3. Let the 14-day window run — this is a Play policy timer, not something any tooling can skip.
4. Only promote to Production once that window passes and testing looks clean.

## 10. Production release

Once Closed testing passes:
1. Create a Production release, upload the same (or a newer, higher-version-code) `.aab`.
2. Write release notes (see below).
3. Submit for review.

### Suggested first release notes
```
First release of Flying Bird — a classic tap-to-fly arcade game. Guide your bird between the pillars, survive as long as you can, and try to beat your own best score.
```

## 11. Store listing content
Already drafted in `PLAY_STORE_LISTING.md` (short/full description, feature bullets) and `STORE_ASSETS_CHECKLIST.md` (required image specs — screenshots/feature graphic/icon still need to be captured from an actual running build).

---

## Summary — what's genuinely left before Production submission

1. **Store screenshots + feature graphic + 512×512 store icon** (see `STORE_ASSETS_CHECKLIST.md`) — need to be captured from a running build.
2. **AdMob account approval** — pending on Google's side; ads are wired with real IDs already and will start serving real inventory automatically once approved.
3. **The 12-tester / 14-day Closed testing window** (Section 9) — a Play policy requirement for new developer accounts, not something this project can shortcut.
4. Everything else in this document is data-entry you can complete directly in Play Console once you're ready.
