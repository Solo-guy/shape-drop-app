// Quảng cáo AdMob: chỉ dùng quảng cáo xen kẽ (interstitial) giữa các lượt chơi.
import { Capacitor } from '@capacitor/core';
import { AdMob, InterstitialAdPluginEvents } from '@capacitor-community/admob';
import { CONFIG } from './config.js';

const native = Capacitor.isNativePlatform();
const plat = Capacitor.getPlatform();
const adId = () => CONFIG.admob[plat === 'ios' ? 'ios' : 'android'].interstitial;

const st = { canRequestAds: false, privacyRequired: false, loaded: false, lastAdAt: Date.now(), runs: 0 };

async function load() {
  if (!st.canRequestAds) return;
  try { await AdMob.prepareInterstitial({ adId: adId(), isTesting: CONFIG.ADS_TESTING }); st.loaded = true; }
  catch (e) { st.loaded = false; }
}

export const ads = {
  state: st,

  async init() {
    if (!native) return;
    try {
      await AdMob.initialize({ initializeForTesting: CONFIG.ADS_TESTING });
      // 1) Đồng ý quyền riêng tư (GDPR/UMP) — bắt buộc với người dùng EU/UK
      let ci = await AdMob.requestConsentInfo();
      if (ci.isConsentFormAvailable && ci.status === 'REQUIRED') ci = await AdMob.showConsentForm();
      st.privacyRequired = ci.privacyOptionsRequirementStatus === 'REQUIRED';
      st.canRequestAds = ci.canRequestAds !== false;
      // 2) iOS: xin quyền theo dõi (App Tracking Transparency)
      if (plat === 'ios') {
        const s = await AdMob.trackingAuthorizationStatus();
        if (s.status === 'notDetermined') await AdMob.requestTrackingAuthorization();
      }
      AdMob.addListener(InterstitialAdPluginEvents.FailedToLoad, () => { st.loaded = false; });
      load();
    } catch (e) { console.warn('[ads] init', e); }
  },

  /** Gọi khi người chơi bấm Chơi lại: cứ mỗi N lượt thua (và cách nhau đủ lâu) thì hiện 1 quảng cáo. */
  async beforeRetry() {
    st.runs++;
    if (!native || !st.loaded) { if (native) load(); return; }
    if (st.runs % CONFIG.interstitialEveryRuns !== 0) return;
    if ((Date.now() - st.lastAdAt) / 1000 < CONFIG.interstitialMinGapSec) return;
    await new Promise(async resolve => {
      const hs = []; let done = false;
      const fin = () => { if (done) return; done = true; hs.forEach(h => h.remove()); resolve(); };
      hs.push(await AdMob.addListener(InterstitialAdPluginEvents.Dismissed, fin));
      hs.push(await AdMob.addListener(InterstitialAdPluginEvents.FailedToShow, fin));
      try { await AdMob.showInterstitial(); } catch (e) { fin(); }
      setTimeout(fin, 90000);
    });
    st.loaded = false; st.lastAdAt = Date.now(); load();
  },

  async showPrivacyOptions() { if (native) try { await AdMob.showPrivacyOptionsForm(); } catch (e) { /* */ } },
};
