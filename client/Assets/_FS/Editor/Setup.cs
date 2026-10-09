using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FS.EditorTools
{
    /// <summary>
    /// Settings, cena e builds por script (mesmo padrao do Rune Relay / COE). Idempotente. Em batch:
    ///   Unity -batchmode -quit -projectPath client -executeMethod FS.EditorTools.Setup.Apply
    ///   Unity -batchmode -quit -projectPath client -executeMethod FS.EditorTools.Setup.BuildWindows (ou BuildAndroidDev)
    /// A cena e' artefato gerado e vazia: o Game nasce sozinho (RuntimeInitializeOnLoadMethod).
    /// </summary>
    public static class Setup
    {
        // ponytail: appId provisorio; depois de publicado na Play nao muda nunca (decisao do idealizador)
        public const string AppId = "br.com.vstack.forgestreet";
        const string Scene = "Assets/_FS/Scenes/Main.unity";

        [MenuItem("FS/Aplicar settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = "Forge Street";
            PlayerSettings.bundleVersion = "0.5.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;      // a Play exige 64 bits
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // ponytail: hipotese ate fixar o aparelho minimo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.predictiveBackSupport = true; // API 36: o voltar chega ao jogo como Esc (lido pelo Input legado, ver activeInputHandler)

            // Windows so' de dev: janela retrato para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            // 2 = Input System + Input Manager legado (sem API publica; vale na proxima abertura do editor, como no COE). O legado e' so
            // para o voltar do Android: com o GameActivity o Input System nao recebe a tecla (testado no emulador API 35; forum Unity 1555368)
            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty handler = ps.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 2) { handler.intValue = 2; ps.ApplyModifiedPropertiesWithoutUndo(); }

            if (!File.Exists(Scene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Scene));
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("FS Setup: ok (" + AppId + ", retrato, Android ARM64 IL2CPP API 26+, Input System)");
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/win/ForgeStreet.exe");
        public static void BuildAndroidDev() => Build(BuildTarget.Android, "Builds/android/ForgeStreet-dev.apk");
        // Emulador do PC (x86_64): o berberis do Android 15 derruba o ARM64 traduzido do Unity/IL2CPP (signal 11, 2026-10-08).
        // ponytail: APK so de teste local; aparelho e Play recebem so ARM64 (BuildAndroidDev).
        // O Build tira o exclude de /lib/x86_64 do mainTemplate.gradle so durante este build e devolve o arquivo. Se o resolver do LevelPlay
        // mexer nos dois arquivos dele (mainTemplate.gradle, ProjectSettings/AndroidResolverDependencies.xml), `git checkout` deles depois.
        public static void BuildAndroidEmu() => Build(BuildTarget.Android, "Builds/android/ForgeStreet-emu.apk", AndroidArchitecture.X86_64);

        static void Build(BuildTarget target, string path, AndroidArchitecture arch = AndroidArchitecture.None)
        {
            Apply();
            if (arch != AndroidArchitecture.None) PlayerSettings.Android.targetArchitectures = arch;
            // o empacotamento incremental do Gradle reaproveita o APK anterior: depois de trocar de branch deixou ~12 MB de buracos e,
            // alternando ARM64/x86_64, saiu APK sem libunity/libil2cpp (crash "libgame.so not found"). Apagar a saida custa ~1 min
            const string gradleOut = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build";
            if (target == BuildTarget.Android && Directory.Exists(gradleOut)) Directory.Delete(gradleOut, true);
            // o mainTemplate versionado e' o do aparelho (o resolver do LevelPlay exclui /lib/x86_64): sem tirar a linha, o APK do
            // emulador sai sem libunity/libil2cpp. Tira so durante o build x86_64 e devolve o arquivo como estava
            const string tpl = "Assets/Plugins/Android/mainTemplate.gradle";
            string tplOrig = arch == AndroidArchitecture.X86_64 && File.Exists(tpl) ? File.ReadAllText(tpl) : null;
            if (tplOrig != null) File.WriteAllText(tpl, System.Text.RegularExpressions.Regex.Replace(tplOrig, @"^[ \t]*exclude \('/lib/x86_64/\*' \+ '\*'\)\r?\n", "",
                System.Text.RegularExpressions.RegexOptions.Multiline));
            BuildReport r;
            try
            {
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = BuildOptions.Development,
                });
            }
            finally { if (tplOrig != null) File.WriteAllText(tpl, tplOrig); }
            Debug.Log($"BuildSummary({target}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            if (arch != AndroidArchitecture.None) { PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; AssetDatabase.SaveAssets(); }   // nao vaza para o build do aparelho
            if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
