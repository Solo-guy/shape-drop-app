using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using ShapeDrop;

/// <summary>
/// Cấu hình và build Shape Drop.
/// - Menu: Shape Drop ▸ Build Windows + Android
/// - Tự chạy 1 lần khi mở dự án nếu có file Assets/Editor/AUTOBUILD.txt
/// - Dòng lệnh: Unity.exe -batchmode -quit -projectPath . -executeMethod ShapeDropBuild.BuildAll
/// Kết quả: Builds/Windows/ShapeDrop.exe, Builds/Android/ShapeDrop.apk, Builds/build-report.txt
/// </summary>
[InitializeOnLoad]
public static class ShapeDropBuild
{
    const string Marker = "Assets/Editor/AUTOBUILD.txt";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string IconPath = "Assets/Art/Icon.png";
    const string AppId = "com.yourstudio.shapedrop";

    static ShapeDropBuild()
    {
        if (Application.isBatchMode || !File.Exists(Marker)) return;
        EditorApplication.delayCall += WaitThenBuild;
    }

    static void WaitThenBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += WaitThenBuild; return; }
        if (!File.Exists(Marker)) return;
        File.Delete(Marker);
        if (File.Exists(Marker + ".meta")) File.Delete(Marker + ".meta");
        AssetDatabase.Refresh();
        BuildAll();
    }

    [MenuItem("Shape Drop/Build Windows + Android")]
    public static void BuildAll()
    {
        var log = new StringBuilder();
        log.AppendLine("Shape Drop build — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " — Unity " + Application.unityVersion);
        Directory.CreateDirectory("Builds");
        try
        {
            Configure(log);
            Build(log, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/ShapeDrop.exe");
            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUserBuildSettings.buildAppBundle = false;
                Build(log, BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/ShapeDrop.apk");
            }
            else log.AppendLine("ANDROID: SKIPPED — chưa cài module Android Build Support.");
        }
        catch (Exception e) { log.AppendLine("EXCEPTION: " + e); }
        File.WriteAllText("Builds/build-report.txt", log.ToString());
        Debug.Log(log.ToString());
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(Path.GetFullPath("Builds/build-report.txt"));
    }

    static void Build(StringBuilder log, BuildTarget target, BuildTargetGroup group, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            targetGroup = group,
            options = BuildOptions.None,
        };
        BuildReport r = BuildPipeline.BuildPlayer(opts);
        var s = r.summary;
        log.AppendLine(target + ": " + s.result + " — " + path + " — " + (s.totalSize / 1048576f).ToString("0.0") + " MB, " + s.totalTime.TotalSeconds.ToString("0") + " s, lỗi: " + s.totalErrors);
        if (s.result != BuildResult.Succeeded)
            foreach (var step in r.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log.AppendLine("   " + m.content);
    }

    static void Configure(StringBuilder log)
    {
        // ---- scene trống: mọi thứ do Bootstrap dựng bằng code ----
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        // ---- thông tin app ----
        PlayerSettings.companyName = "YourStudio";
        PlayerSettings.productName = "Shape Drop";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        ConfigureInputHandler(log);

        // ---- Windows: cửa sổ dọc, kéo giãn được ----
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

        // ---- Android: 64-bit (bắt buộc với Google Play) ----
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 1;

        // ---- icon pixel ----
        var icon = MakeIcon();
        if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        AssetDatabase.SaveAssets();
        log.AppendLine("Configure: OK");
    }

    /// <summary>
    /// Code hỗ trợ cả Input System mới lẫn Input Manager cũ. Nếu dự án chưa có gói Input System
    /// thì bật Input Manager cũ (0) để không bị lỗi; nếu có gói thì dùng chế độ "Both" (2).
    /// </summary>
    static void ConfigureInputHandler(StringBuilder log)
    {
        bool hasPkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") != null;
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) { log.AppendLine("Input: không đọc được ProjectSettings"); return; }
        var so = new SerializedObject(assets[0]);
        var p = so.FindProperty("activeInputHandler");
        if (p == null) { log.AppendLine("Input: không có activeInputHandler"); return; }
        int want = hasPkg ? 2 : 0;
        if (p.intValue != want) { p.intValue = want; so.ApplyModifiedPropertiesWithoutUndo(); }
        log.AppendLine("Input: gói InputSystem=" + hasPkg + ", activeInputHandler=" + want);
    }

    /// <summary>Vẽ icon 32x32 (tường gạch có lỗ ngôi sao + nhân vật ngôi sao), phóng to 16 lần.</summary>
    static Texture2D MakeIcon()
    {
        const int G = 32, K = 16;
        var c = new Color32[G * G];
        Func<string, Color32> H = Px.Hex;
        for (int y = 0; y < G; y++)
            for (int x = 0; x < G; x++)
            {
                Color32 col = H("#1a1c2c");
                if (y > 20) col = H("#29366f");
                if (y >= 27) col = H("#257179");
                if (y == 27) col = H("#38b764");
                if (y >= 3 && y < 15)
                {
                    int r = (y - 3) / 4, yy = (y - 3) % 4, xx = (x + (r % 2 == 1 ? 3 : 0)) % 7;
                    col = yy == 3 || xx == 6 ? H("#333c57") : (yy == 0 || xx == 0 ? H("#94b0c2") : H("#566c86"));
                }
                c[y * G + x] = col;
            }
        var m = Shapes.Masks[4];      // ngôi sao 15x15
        for (int y = 0; y < 15; y++) for (int x = 0; x < 15; x++) if (m[y * 15 + x] && 1 + y < 15) { int Y = 1 + y; if (Y >= 3) c[Y * G + 8 + x] = H("#0d0e17"); }
        var spr = Shapes.Sprites[4];  // 17x17
        for (int y = 0; y < 17; y++) for (int x = 0; x < 17; x++) { var p = spr[y * 17 + x]; if (p.a > 0) c[(14 + y) * G + 7 + x] = new Color32(p.r, p.g, p.b, 255); }

        var tex = new Texture2D(G * K, G * K, TextureFormat.RGBA32, false);
        var big = new Color32[G * K * G * K];
        for (int y = 0; y < G * K; y++)
            for (int x = 0; x < G * K; x++)
                big[(G * K - 1 - y) * G * K + x] = c[(y / K) * G + x / K];   // lật dọc cho Texture2D
        tex.SetPixels32(big); tex.Apply();
        Directory.CreateDirectory("Assets/Art");
        File.WriteAllBytes(IconPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(IconPath);
        var imp = (TextureImporter)AssetImporter.GetAtPath(IconPath);
        if (imp != null) { imp.filterMode = FilterMode.Point; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }
}
