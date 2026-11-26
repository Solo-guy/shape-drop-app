using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && HAS_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace ShapeDrop
{
    /// <summary>Khởi tạo toàn bộ game bằng code — không cần dựng scene hay prefab trong Editor.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (UnityEngine.Object.FindFirstObjectByType<GameApp>() != null) return;
            var go = new GameObject("ShapeDrop");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }
    }

    /// <summary>Kéo chuột / ngón tay trên màn hình game để di chuyển.</summary>
    public class DragArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<float> OnDragPixels;
        public void OnPointerDown(PointerEventData e) { }
        public void OnDrag(PointerEventData e) { if (OnDragPixels != null) OnDragPixels(e.delta.x); }
    }

    public class GameApp : MonoBehaviour
    {
        static readonly Color Bg = Hex("#1a1c2c"), Panel = Hex("#333c57"), Ink = Hex("#f4f4f4"), MuteC = Hex("#94b0c2"),
            HotC = Hex("#ffcd75"), Org = Hex("#ef7d57"), Red = Hex("#b13e53"), Cyan = Hex("#73eff7"), Grn = Hex("#38b764"), Blk = Hex("#0d0e17");

        GameCore game;
        Sfx sfx;
        Font font;
        Canvas canvas;
        RectTransform safeRoot, gameArea, screenRect, progBar, gapBar;
        RawImage screenImg;
        Text lvText, spdText, bestText, finalLv, overMsg, toastText, soundBtnText, langBtnText;
        GameObject menu, pauseOv, overOv, winOv, howOv, pauseBtn;
        CanvasGroup toastGroup;
        float toastT, overAt;
        int best;
        bool overIsRecord;
        Rect lastSafe;
        Vector2 lastAreaSize;
        readonly System.Collections.Generic.List<Action> relabel = new System.Collections.Generic.List<Action>();

        static Color Hex(string h) { var c = Px.Hex(h); return new Color32(c.r, c.g, c.b, 255); }

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            best = PlayerPrefs.GetInt("best", 0);
            string lang = PlayerPrefs.GetString("lang", "");
            Loc.Lang = lang == "vi" || lang == "en" ? lang : (Application.systemLanguage == SystemLanguage.Vietnamese ? "vi" : "en");

            var cam = Camera.main;
            if (cam == null) { var cgo = new GameObject("Camera"); cam = cgo.AddComponent<Camera>(); cgo.transform.SetParent(transform); }
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.cullingMask = 0;

            sfx = gameObject.AddComponent<Sfx>();
            sfx.Muted = PlayerPrefs.GetInt("muted", 0) == 1;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            game = new GameCore();
            game.OnHud = Hud;
            game.OnPass = L =>
            {
                sfx.Pass();
                if (L > best) { best = L; PlayerPrefs.SetInt("best", best); }
                if ((L - 1) % 10 == 0) { sfx.Up(); Toast(Loc.Get("faster") + Difficulty.SpeedMult(L).ToString("0.00")); }
                if (L == Difficulty.MaxLevel) Toast(Loc.Get("last"));
            };
            game.OnMorph = () => sfx.Morph();
            game.OnHit = () =>
            {
                sfx.Die();
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            };
            game.OnDie = GameOver;
            game.OnWin = () => { best = Math.Max(best, Difficulty.MaxLevel); PlayerPrefs.SetInt("best", best); PlayerPrefs.Save(); sfx.Win(); Show(winOv); };

            BuildUI();
            Relabel();
            Hud(1);
            Show(menu);
        }

        /* ================= giao diện dựng bằng code ================= */
        RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        static void Fill(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
        }
        Image Img(RectTransform r, Color c) { var i = r.gameObject.AddComponent<Image>(); i.color = c; return i; }
        Text Txt(Transform parent, string name, int size, Color c, TextAnchor a, FontStyle st = FontStyle.Bold)
        {
            var r = Node(name, parent); Fill(r);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }
        void Label(Text t, string key) { relabel.Add(() => t.text = Loc.Get(key)); }

        /// <summary>Nút kiểu pixel: nền đặc, viền tối, bóng đổ.</summary>
        Text Btn(RectTransform parent, string name, Vector2 pos, Vector2 size, Color c, Action onClick, string key = null, int fs = 44)
        {
            var r = Node(name, parent);
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = pos;
            var img = Img(r, c);
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = Blk; sh.effectDistance = new Vector2(8, -8);
            var ol = r.gameObject.AddComponent<Outline>(); ol.effectColor = Blk; ol.effectDistance = new Vector2(4, -4);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var cb = b.colors; cb.pressedColor = new Color(.8f, .8f, .8f); b.colors = cb;
            b.onClick.AddListener(() => onClick());
            var t = Txt(r, "Label", fs, Bg, TextAnchor.MiddleCenter);
            if (key != null) Label(t, key);
            return t;
        }

        GameObject Overlay(string name, Color tint)
        {
            var r = Node(name, gameArea); Fill(r);
            var img = Img(r, tint); img.raycastTarget = true;
            return r.gameObject;
        }

        Text Title(GameObject ov, string key, float y, int size, Color c)
        {
            var t = Txt(ov.transform, "Title", size, c, TextAnchor.MiddleCenter);
            var r = t.rectTransform; r.anchorMin = new Vector2(0, .5f); r.anchorMax = new Vector2(1, .5f); r.sizeDelta = new Vector2(-80, size * 2.6f); r.anchoredPosition = new Vector2(0, y);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = Red; sh.effectDistance = new Vector2(6, -6);
            if (key != null) Label(t, key);
            return t;
        }

        void BuildUI()
        {
            // EventSystem tương thích cả Input System mới và Input Manager cũ
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform);
#if ENABLE_INPUT_SYSTEM && HAS_INPUTSYSTEM
                var mod = es.AddComponent<InputSystemUIInputModule>();
                mod.AssignDefaultActions();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            cgo.AddComponent<GraphicRaycaster>();

            var bgR = Node("Background", cgo.transform); Fill(bgR); Img(bgR, Color.black).raycastTarget = false;
            safeRoot = Node("SafeArea", cgo.transform); Fill(safeRoot);

            // ---------- HUD ----------
            const float hudH = 170;
            var hud = Node("HUD", safeRoot);
            hud.anchorMin = new Vector2(0, 1); hud.anchorMax = Vector2.one; hud.pivot = new Vector2(.5f, 1); hud.sizeDelta = new Vector2(0, hudH); hud.anchoredPosition = Vector2.zero;
            Img(hud, Panel);
            string[] keys = { "lvl", "spd", "best" };
            Text[] vals = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var col = Node("Col" + i, hud);
                col.anchorMin = new Vector2(i * .27f, 0); col.anchorMax = new Vector2(i * .27f + .27f, 1); col.offsetMin = new Vector2(30, 24); col.offsetMax = new Vector2(0, -16);
                var lab = Txt(col, "Label", 32, MuteC, TextAnchor.UpperLeft, FontStyle.Normal); Label(lab, keys[i]);
                vals[i] = Txt(col, "Value", 60, Ink, TextAnchor.LowerLeft);
            }
            lvText = vals[0]; spdText = vals[1]; bestText = vals[2];
            var pr = Node("PauseBtnHolder", hud); pr.anchorMin = new Vector2(1, .5f); pr.anchorMax = new Vector2(1, .5f); pr.sizeDelta = new Vector2(120, 120); pr.anchoredPosition = new Vector2(-90, 8);
            var pt = Btn(pr, "Pause", Vector2.zero, new Vector2(110, 110), MuteC, TogglePause, null, 50); pt.text = "II";
            pauseBtn = pr.gameObject;
            var bars = Node("Bars", hud); bars.anchorMin = Vector2.zero; bars.anchorMax = new Vector2(1, 0); bars.pivot = new Vector2(.5f, 0); bars.sizeDelta = new Vector2(0, 14);
            Img(bars, Blk);
            progBar = Node("Progress", bars); progBar.anchorMin = new Vector2(0, .5f); progBar.anchorMax = new Vector2(0, 1); progBar.offsetMin = progBar.offsetMax = Vector2.zero; Img(progBar, Grn);
            gapBar = Node("Gap", bars); gapBar.anchorMin = Vector2.zero; gapBar.anchorMax = new Vector2(0, .5f); gapBar.offsetMin = gapBar.offsetMax = Vector2.zero; Img(gapBar, Cyan);

            // ---------- vùng game ----------
            gameArea = Node("GameArea", safeRoot); Fill(gameArea, 0, 0, 0, hudH);
            screenRect = Node("Screen", gameArea); screenRect.anchorMin = screenRect.anchorMax = new Vector2(.5f, .5f);
            screenImg = screenRect.gameObject.AddComponent<RawImage>();
            var drag = screenRect.gameObject.AddComponent<DragArea>();
            drag.OnDragPixels = dx =>
            {
                float pxPerGame = screenRect.rect.width * canvas.scaleFactor / GameCore.W;
                if (pxPerGame > 0) game.DragBy(dx / pxPerGame);
            };

            // ---------- menu ----------
            menu = Overlay("Menu", new Color(.1f, .11f, .17f, .55f));
            var mb = menu.AddComponent<Button>(); mb.transition = Selectable.Transition.None; mb.onClick.AddListener(Play);
            var t1 = Title(menu, null, 430, 150, HotC); t1.text = "SHAPE\nDROP"; t1.rectTransform.sizeDelta = new Vector2(-40, 420);
            var tag = Txt(menu.transform, "Tag", 40, MuteC, TextAnchor.MiddleCenter, FontStyle.Normal); Label(tag, "tag");
            tag.rectTransform.anchorMin = new Vector2(0, .5f); tag.rectTransform.anchorMax = new Vector2(1, .5f); tag.rectTransform.sizeDelta = new Vector2(-80, 80); tag.rectTransform.anchoredPosition = new Vector2(0, 150);
            var tap = Txt(menu.transform, "Tap", 64, Ink, TextAnchor.MiddleCenter); Label(tap, "tap");
            tap.rectTransform.anchorMin = new Vector2(0, .5f); tap.rectTransform.anchorMax = new Vector2(1, .5f); tap.rectTransform.sizeDelta = new Vector2(0, 100); tap.rectTransform.anchoredPosition = new Vector2(0, -20);
            tap.gameObject.AddComponent<Blink>();
            var mrow = Node("Row", menu.transform); mrow.anchorMin = mrow.anchorMax = new Vector2(.5f, .5f); mrow.sizeDelta = new Vector2(1000, 400); mrow.anchoredPosition = new Vector2(0, -330);
            langBtnText = Btn(mrow, "Lang", new Vector2(-250, 90), new Vector2(440, 120), MuteC, ToggleLang, "langBtn", 42);
            soundBtnText = Btn(mrow, "Sound", new Vector2(250, 90), new Vector2(440, 120), MuteC, ToggleSound, null, 42);
            relabel.Add(() => soundBtnText.text = Loc.Get(sfx.Muted ? "soundOff" : "soundOn"));
            Btn(mrow, "How", new Vector2(Application.isMobilePlatform ? 0 : -250, -80), new Vector2(440, 120), MuteC, () => Show(howOv), "how", 42);
            if (!Application.isMobilePlatform) Btn(mrow, "Quit", new Vector2(250, -80), new Vector2(440, 120), MuteC, Application.Quit, "quit", 42);

            // ---------- tạm dừng ----------
            pauseOv = Overlay("Pause", new Color(.1f, .11f, .17f, .85f));
            Title(pauseOv, "pause", 200, 96, HotC);
            Btn((RectTransform)pauseOv.transform, "Resume", new Vector2(0, -20), new Vector2(620, 150), HotC, Resume, "resume", 52);
            Btn((RectTransform)pauseOv.transform, "Menu", new Vector2(0, -220), new Vector2(620, 130), MuteC, ToMenu, "menu", 44);

            // ---------- thua ----------
            overOv = Overlay("Over", new Color(.1f, .11f, .17f, .86f));
            Title(overOv, "overT", 420, 100, HotC);
            var os = Txt(overOv.transform, "Sub", 44, Ink, TextAnchor.MiddleCenter, FontStyle.Normal); Label(os, "overS");
            os.rectTransform.anchorMin = new Vector2(0, .5f); os.rectTransform.anchorMax = new Vector2(1, .5f); os.rectTransform.sizeDelta = new Vector2(0, 80); os.rectTransform.anchoredPosition = new Vector2(0, 280);
            finalLv = Txt(overOv.transform, "Level", 170, Cyan, TextAnchor.MiddleCenter);
            finalLv.rectTransform.anchorMin = new Vector2(0, .5f); finalLv.rectTransform.anchorMax = new Vector2(1, .5f); finalLv.rectTransform.sizeDelta = new Vector2(0, 220); finalLv.rectTransform.anchoredPosition = new Vector2(0, 120);
            overMsg = Txt(overOv.transform, "Msg", 46, MuteC, TextAnchor.MiddleCenter);
            overMsg.rectTransform.anchorMin = new Vector2(0, .5f); overMsg.rectTransform.anchorMax = new Vector2(1, .5f); overMsg.rectTransform.sizeDelta = new Vector2(0, 80); overMsg.rectTransform.anchoredPosition = new Vector2(0, -20);
            Btn((RectTransform)overOv.transform, "Again", new Vector2(0, -200), new Vector2(640, 160), HotC, Retry, "again", 56);
            Btn((RectTransform)overOv.transform, "Menu", new Vector2(0, -400), new Vector2(640, 130), MuteC, ToMenu, "menu", 44);

            // ---------- thắng ----------
            winOv = Overlay("Win", new Color(.1f, .11f, .17f, .86f));
            Title(winOv, "winT", 300, 84, HotC);
            var ws = Txt(winOv.transform, "Sub", 44, Ink, TextAnchor.MiddleCenter, FontStyle.Normal); Label(ws, "winS");
            ws.rectTransform.anchorMin = new Vector2(0, .5f); ws.rectTransform.anchorMax = new Vector2(1, .5f); ws.rectTransform.sizeDelta = new Vector2(-120, 200); ws.rectTransform.anchoredPosition = new Vector2(0, 100);
            Btn((RectTransform)winOv.transform, "Endless", new Vector2(0, -150), new Vector2(640, 150), HotC, () => { game.Endless(); Show(null); Toast(Loc.Get("endlessT")); }, "endless", 52);
            Btn((RectTransform)winOv.transform, "Menu", new Vector2(0, -340), new Vector2(640, 130), MuteC, ToMenu, "menu", 44);

            // ---------- cách chơi ----------
            howOv = Overlay("How", new Color(.1f, .11f, .17f, .95f));
            Title(howOv, "how", 620, 84, HotC);
            var ht = Txt(howOv.transform, "Text", 42, Ink, TextAnchor.UpperLeft, FontStyle.Normal); Label(ht, "howText");
            ht.rectTransform.anchorMin = new Vector2(0, .5f); ht.rectTransform.anchorMax = new Vector2(1, .5f); ht.rectTransform.sizeDelta = new Vector2(-140, 900); ht.rectTransform.anchoredPosition = new Vector2(0, 40);
            Btn((RectTransform)howOv.transform, "Close", new Vector2(0, -560), new Vector2(560, 140), HotC, () => Show(menu), "close", 50);

            // ---------- thông báo nổi ----------
            toastText = Txt(gameArea, "Toast", 60, HotC, TextAnchor.MiddleCenter);
            toastText.rectTransform.anchorMin = new Vector2(0, .62f); toastText.rectTransform.anchorMax = new Vector2(1, .62f); toastText.rectTransform.sizeDelta = new Vector2(-60, 200);
            var tol = toastText.gameObject.AddComponent<Outline>(); tol.effectColor = Blk; tol.effectDistance = new Vector2(5, -5);
            toastGroup = toastText.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
        }

        void Relabel() { foreach (var a in relabel) a(); if (game.state == State.Over) SetOverMsg(); }

        /* ================= luồng game ================= */
        void Show(GameObject ov)
        {
            foreach (var o in new[] { menu, pauseOv, overOv, winOv, howOv }) o.SetActive(o == ov);
            pauseBtn.SetActive(ov == null || ov == pauseOv);
        }

        void Hud(int L)
        {
            lvText.text = L + "<size=34><color=#94b0c2>/" + Difficulty.MaxLevel + "</color></size>";
            lvText.supportRichText = true;
            spdText.text = "x" + Difficulty.SpeedMult(L).ToString("0.00");
            bestText.text = best.ToString();
            progBar.anchorMax = new Vector2(Mathf.Clamp01((L - 1f) / (Difficulty.MaxLevel - 1)), 1);
            gapBar.anchorMax = new Vector2(Mathf.Max(.01f, (Difficulty.Tolerance(L) - 1.2f) / Difficulty.MaxMargin), .5f);
        }

        void Toast(string s) { toastText.text = s; toastT = 1.4f; }

        void Play()
        {
            sfx.Start_();
            Show(null);
            FitScreen(true);
            game.Start();
        }

        void Retry()
        {
            if (Time.unscaledTime - overAt < .35f) return;     // tránh bấm nhầm ngay khi vừa thua
            Ads.BeforeRetry(Play);
        }

        void GameOver(int L)
        {
            overAt = Time.unscaledTime;
            overIsRecord = L >= best && L > 1;
            if (L > best) { best = L; PlayerPrefs.SetInt("best", best); }
            PlayerPrefs.Save();
            finalLv.text = L.ToString();
            SetOverMsg();
            Hud(L);
            Show(overOv);
        }
        void SetOverMsg()
        {
            overMsg.text = overIsRecord ? Loc.Get("newRec") : Loc.Get("recIs") + best;
            overMsg.color = overIsRecord ? HotC : MuteC;
        }

        void ToMenu() { game.ToMenu(); Show(menu); Hud(1); FitScreen(true); }
        void Resume() { game.Resume(); Show(null); }
        void TogglePause() { if (game.state == State.Pause) Resume(); else if (game.Pause()) Show(pauseOv); }
        void ToggleLang() { Loc.Lang = Loc.Lang == "vi" ? "en" : "vi"; PlayerPrefs.SetString("lang", Loc.Lang); Relabel(); }
        void ToggleSound() { sfx.Muted = !sfx.Muted; PlayerPrefs.SetInt("muted", sfx.Muted ? 1 : 0); Relabel(); }

        /* ================= khung hình ================= */
        void FitScreen(bool force)
        {
            var size = gameArea.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            if (force || game.state == State.Menu || game.state == State.Over)
            {
                if (Mathf.RoundToInt(GameCore.W * size.y / size.x) != game.H || game.px == null) game.Resize(GameCore.W * size.y / size.x);
            }
            float scale = Mathf.Min(size.x / GameCore.W, size.y / game.H);
            screenRect.sizeDelta = new Vector2(GameCore.W * scale, game.H * scale);
            screenImg.texture = game.px.tex;
        }

        void ApplySafeArea()
        {
            var sa = Screen.safeArea;
            if (sa == lastSafe || Screen.width == 0) return;
            lastSafe = sa;
            safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        void OnApplicationPause(bool paused) { if (paused && game.Pause()) Show(pauseOv); }
        void OnApplicationFocus(bool focus) { if (!focus && !Application.isMobilePlatform && game.Pause()) Show(pauseOv); }

        /* ================= vòng lặp ================= */
        void Update()
        {
            ApplySafeArea();
            var area = gameArea.rect.size;
            if (area != lastAreaSize) { lastAreaSize = area; FitScreen(false); }

            ReadKeyboard();
            game.Update(Mathf.Min(Time.unscaledDeltaTime, .05f));
            game.Render();
            if (screenImg.texture != game.px.tex) screenImg.texture = game.px.tex;

            if (toastT > 0)
            {
                toastT -= Time.unscaledDeltaTime;
                float k = 1.4f - toastT;
                toastGroup.alpha = k < .2f ? k / .2f : (toastT < .3f ? toastT / .3f : 1f);
                toastText.rectTransform.anchoredPosition = new Vector2(0, k < 1.1f ? 0 : (k - 1.1f) * 60);
            }
            else toastGroup.alpha = 0;
        }

        void ReadKeyboard()
        {
            bool left, right, slow, confirm, back, p;
#if ENABLE_INPUT_SYSTEM && HAS_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb == null) { left = right = slow = confirm = p = false; back = false; }
            else
            {
                left = kb.leftArrowKey.isPressed || kb.aKey.isPressed;
                right = kb.rightArrowKey.isPressed || kb.dKey.isPressed;
                slow = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                confirm = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                back = kb.escapeKey.wasPressedThisFrame;
                p = kb.pKey.wasPressedThisFrame;
            }
#else
            left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            slow = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            confirm = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            back = Input.GetKeyDown(KeyCode.Escape);       // nút Back trên Android
            p = Input.GetKeyDown(KeyCode.P);
#endif
            game.SetDir((right ? 1 : 0) - (left ? 1 : 0));
            game.SetSlow(slow);

            if (confirm)
            {
                if (game.state == State.Menu && menu.activeSelf) Play();
                else if (game.state == State.Over) Retry();
                else if (game.state == State.Pause) Resume();
            }
            if (p) TogglePause();
            if (back)
            {
                if (game.state == State.Play) TogglePause();
                else if (howOv.activeSelf) Show(menu);
                else if (game.state == State.Pause || game.state == State.Over || game.state == State.Won) ToMenu();
                else if (game.state == State.Menu && Application.platform == RuntimePlatform.Android) Application.Quit();
            }
        }
    }

    /// <summary>Chữ nhấp nháy kiểu arcade.</summary>
    public class Blink : MonoBehaviour
    {
        Text t;
        void Awake() { t = GetComponent<Text>(); }
        void Update() { if (t != null) t.enabled = Mathf.Repeat(Time.unscaledTime, 1f) < .6f; }
    }
}
