# Privacy Policy — One More

**Last updated: [DATE YOU PUBLISH THIS]**

This Privacy Policy describes how the "One More" mobile game ("the App", "we", "us") handles information. It is based on exactly what the App and the third-party services it uses actually do as of this writing — nothing here describes a feature the App does not have.

> **Before publishing:** replace every `[BRACKETED PLACEHOLDER]` below with real information. Nothing was invented on your behalf — these are the fields only you can fill in (your legal/developer name, contact address, jurisdiction, etc.).

---

## 1. Who this policy covers

This policy applies to the "One More" app, developed by **[YOUR NAME OR STUDIO/COMPANY NAME]**, available on Google Play (and, if later published, the Apple App Store).

Contact: **[YOUR SUPPORT EMAIL ADDRESS]**
Developer address (if required by your jurisdiction/store): **[YOUR ADDRESS, OR STATE "NOT APPLICABLE" IF NOT REQUIRED]**

## 2. What information is collected

**One More does not require an account, and does not ask you for your name, email, or any personal contact information.**

### 2.1 Information the App itself stores (on your device only)
- Your best score, total games played, and total score — saved locally on your device only (`Application.persistentDataPath`, a private app-storage location). This data is **never transmitted anywhere** by the App itself; it exists only so your progress persists between sessions on the same device. Uninstalling the App deletes it.
- Your sound/music/haptics on-off preferences — same local-only storage.

### 2.2 Information collected by third-party services the App uses

**Google AdMob (advertising)**
The App shows ads (rewarded video and interstitial ads) via Google's AdMob/Google Mobile Ads SDK. To do this, Google's SDK may collect:
- Advertising identifiers (e.g. Android Advertising ID / Google Advertising ID)
- Device information (device type, OS version, general device attributes)
- App interaction data related to ad delivery and measurement (e.g. which ads were shown/clicked)
- Approximate location derived from IP address (for regional ad relevance/compliance, not precise GPS location — **this App does not request or use device GPS/location permissions**)

This data is collected and processed by Google, not by us directly, under Google's own privacy policy: https://policies.google.com/privacy

**Consent (EEA/UK/other applicable regions)**
The App uses Google's User Messaging Platform (UMP) to ask for your consent to personalized advertising where required by law (for example, under GDPR for users in the European Economic Area and UK). If you decline or an applicable law otherwise requires it, you may still see ads, but they will not be personalized based on your advertising identifier. You can change this choice later from **[LOCATION OF YOUR "RESET AD CONSENT" SETTING, ONCE YOU ADD ONE — SEE DEPLOYMENT_STATUS.md]**.

### 2.3 What is NOT collected
The App does not collect and does not request: your name, email address, phone number, contacts, precise/GPS location, photos, microphone/camera access, or any account/login information. As of this version, no analytics or crash-reporting service is actively transmitting data off-device (see Section 3).

## 3. Analytics

The App has an internal event log (game started, game over, score milestones, ad interactions, etc.) used only to understand gameplay for our own development purposes. **As of this version, this log is not connected to any external analytics service** — events are recorded temporarily in memory on your device and are not transmitted anywhere. If this changes in a future update (for example, by adding Firebase Analytics), this policy will be updated first, and this section will name the service and what it collects.

## 4. Crash / diagnostic information

The App does not currently integrate a crash-reporting SDK. If diagnostic/crash reporting is added in a future update, this policy will be updated to disclose it before that update is released.

## 5. Cookies / web technologies

The App is a native mobile game, not a website, and does not use cookies. Third-party SDKs (AdMob) may use comparable device-level identifiers as described in Section 2.2.

## 6. Children's privacy

One More is not directed at children under 13 (or the equivalent minimum age in your region) and we do not knowingly collect personal information from children. If you believe a child has provided us with information, contact us at **[YOUR SUPPORT EMAIL]** and we will address it. **[IF YOU LATER INTEND TO TARGET THE APP AT CHILDREN VIA GOOGLE PLAY FAMILIES / APP STORE KIDS CATEGORY, YOU MUST FOLLOW A DIFFERENT, STRICTER COMPLIANCE PATH — INCLUDING RESTRICTING ADS TO NON-PERSONALIZED/CHILD-DIRECTED ADS ONLY. THIS DRAFT ASSUMES A GENERAL-AUDIENCE APP, NOT A CHILDREN'S APP.]**

## 7. Data retention

- On-device save data (Section 2.1) persists until you uninstall the App or clear its data.
- Advertising-related data collected by Google (Section 2.2) is retained and governed by Google's own retention policies, not ours.

## 8. Your choices

- You can reset your ad-personalization consent choice at any time via **[SETTINGS LOCATION, ONCE ADDED]**.
- You can request that Google limit ad tracking via your device's own OS-level advertising settings (e.g. Android Settings → Google → Ads → "Opt out of Ads Personalization").
- Uninstalling the App removes all locally-stored save data (Section 2.1) from your device.

## 9. Third-party services this App uses

| Service | Purpose | Their privacy policy |
|---|---|---|
| Google AdMob / Google Mobile Ads SDK | Serving ads (rewarded + interstitial) | https://policies.google.com/privacy |
| Google User Messaging Platform (UMP) | Gathering ad-consent choices where legally required | https://policies.google.com/privacy |

## 10. Changes to this policy

We may update this policy as the App changes (for example, if we add an analytics or crash-reporting SDK). We will update the "Last updated" date above when we do. Continued use of the App after an update constitutes acceptance of the revised policy.

## 11. Contact

Questions about this policy or your data: **[YOUR SUPPORT EMAIL ADDRESS]**

---

### Publishing note (delete this section once done)
This file is a local draft, not a public policy. Google Play (and Apple, if you publish there) requires a **live, public HTTPS URL** to a privacy policy — this Markdown file in your project repo does not satisfy that requirement on its own. Publish the finished text somewhere with a stable public URL (a simple static page, a GitHub Pages page, a page on your own site, etc.), fill in every bracketed placeholder first, and put that URL into Play Console under **App content → Privacy policy**. See `PLAY_CONSOLE_MANUAL_STEPS.md`.

**PRIVACY POLICY PUBLIC URL REQUIRED** before this app can be published on Google Play.
