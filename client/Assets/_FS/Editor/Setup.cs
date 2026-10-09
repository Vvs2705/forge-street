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
        public const string Version = "0.6.0";   // SemVer; o versionCode do release sai daqui (VersionCode)
        const string Scene = "Assets/_FS/Scenes/Main.unity";

        [MenuItem("FS/Aplicar settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = "Forge Street";
            PlayerSettings.bundleVersion = Version;
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
        // Play: AAB sem Development, ARM64, target 36, assinado com a chave de upload lida SO de variavel de ambiente.
        // Os settings de release valem so durante o build (ProjectSettings volta ao do Dev). Conferencia: client/tools/verify.sh release
        public static void BuildAndroidRelease() => Build(BuildTarget.Android, $"Builds/android/ForgeStreet-{Version}.aab", release: true);

        // versionCode = maior*10000 + menor*100 + patch (0.6.0 = 600, 1.2.3 = 10203): cresce com o SemVer; o 1 das builds de dev fica abaixo
        public static int VersionCode(string v)
        {
            int[] p = System.Array.ConvertAll(v.Split('.'), int.Parse);
            if (p.Length != 3 || p[1] > 99 || p[2] > 99) throw new System.ArgumentException("versao fora do esquema X.Y.Z com Y e Z ate 99: " + v);
            return p[0] * 10000 + p[1] * 100 + p[2];
        }

        // sem a variavel o build falha: o release nunca cai na chave de debug. O valor nunca vai para log
        static string Env(string name)
        {
            string v = System.Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(v)) throw new System.Exception($"release sem assinatura: defina {name} (FS_KEYSTORE, FS_KEYSTORE_PASS, FS_KEY_ALIAS, FS_KEY_PASS)");
            return v;
        }

        static void Build(BuildTarget target, string path, AndroidArchitecture arch = AndroidArchitecture.None, bool release = false)
        {
            Apply();
            const string tpl = "Assets/Plugins/Android/mainTemplate.gradle";
            string tplOrig = null;
            BuildReport r = null;
            bool aab = EditorUserBuildSettings.buildAppBundle;
            int code = PlayerSettings.Android.bundleVersionCode;
            try
            {
                if (release)
                {
                    string ks = Env("FS_KEYSTORE");
                    if (!File.Exists(ks)) throw new FileNotFoundException("FS_KEYSTORE aponta para um arquivo que nao existe: " + ks);
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = ks;
                    PlayerSettings.Android.keystorePass = Env("FS_KEYSTORE_PASS");
                    PlayerSettings.Android.keyaliasName = Env("FS_KEY_ALIAS");
                    PlayerSettings.Android.keyaliasPass = Env("FS_KEY_PASS");
                    PlayerSettings.Android.bundleVersionCode = VersionCode(Version);
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;   // fixo: exigencia da Play desde 31/08/2026
                    EditorUserBuildSettings.buildAppBundle = true;
                }
                if (arch != AndroidArchitecture.None) PlayerSettings.Android.targetArchitectures = arch;
                // o empacotamento incremental do Gradle reaproveita o APK anterior: depois de trocar de branch deixou ~12 MB de buracos e,
                // alternando ARM64/x86_64, saiu APK sem libunity/libil2cpp (crash "libgame.so not found"). Apagar a saida custa ~1 min
                const string gradleOut = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build";
                if (target == BuildTarget.Android && Directory.Exists(gradleOut)) Directory.Delete(gradleOut, true);   // arquivo preso (daemon do Gradle): IOException aborta, o finally devolve tudo
                // o mainTemplate versionado e' o do aparelho (o resolver do LevelPlay exclui /lib/x86_64): sem tirar a linha, o APK do
                // emulador sai sem libunity/libil2cpp. Tira so durante o build x86_64 e devolve o arquivo como estava
                if (arch == AndroidArchitecture.X86_64 && File.Exists(tpl))
                {
                    tplOrig = File.ReadAllText(tpl);
                    string semX86 = System.Text.RegularExpressions.Regex.Replace(tplOrig, @"^[ \t]*exclude \('/lib/x86_64/\*' \+ '\*'\)\r?\n", "",
                        System.Text.RegularExpressions.RegexOptions.Multiline);
                    if (semX86 == tplOrig) Debug.LogError("FS Setup: exclude de /lib/x86_64 nao achado no mainTemplate.gradle; o APK do emulador pode sair sem .so");
                    File.WriteAllText(tpl, semX86);
                }
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = release ? BuildOptions.None : BuildOptions.Development,
                });
                Debug.Log($"BuildSummary({target}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            }
            catch (System.Exception e) { Debug.LogError("FS Setup: build abortado: " + e.Message); }
            finally
            {
                if (tplOrig != null) File.WriteAllText(tpl, tplOrig);
                if (arch != AndroidArchitecture.None) { PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; AssetDatabase.SaveAssets(); }   // nao vaza para o build do aparelho
                if (release)   // devolve o Dev: APK, target automatico, chave de debug, sem caminho nem senha no ProjectSettings
                {
                    EditorUserBuildSettings.buildAppBundle = aab;
                    PlayerSettings.Android.bundleVersionCode = code;
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                    PlayerSettings.Android.useCustomKeystore = false;
                    PlayerSettings.Android.keystoreName = PlayerSettings.Android.keyaliasName = "";
                    PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = "";
                    AssetDatabase.SaveAssets();
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(r != null && r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
