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
            PlayerSettings.bundleVersion = "0.4.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;      // a Play exige 64 bits
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // ponytail: hipotese ate fixar o aparelho minimo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.predictiveBackSupport = true; // API 36: o voltar chega ao jogo como Esc

            // Windows so' de dev: janela retrato para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            // 1 = so' Input System (sem API publica; vale na proxima abertura do editor, como no COE).
            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty handler = ps.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 1) { handler.intValue = 1; ps.ApplyModifiedPropertiesWithoutUndo(); }

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

        static void Build(BuildTarget target, string path)
        {
            Apply();
            BuildReport r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = path,
                target = target,
                options = BuildOptions.Development,
            });
            Debug.Log($"BuildSummary({target}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
