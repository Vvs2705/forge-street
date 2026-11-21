#if UNITY_ANDROID
// so com o alvo Android: o UnityEditor.Android vem do modulo Android, que as imagens do CI de EditMode e Windows nao tem.
// O verify.sh e o game-ci abrem o Unity com -buildTarget Android para os builds Android, entao o hook sempre compila neles.
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace FS.EditorTools
{
    /// <summary>
    /// D-PRIV-2 (docs/PRIVACIDADE.md §4, docs/PUBLICACAO_PLAY.md §9): tira do manifesto mesclado a permissao
    /// ACCESS_ADSERVICES_TOPICS que os SDKs de anuncio trazem (API Topics do Privacy Sandbox = segmentacao por interesse).
    /// O jogo pede anuncio nao personalizado para menor de 18 e para quem recusou; com a permissao no manifesto o SDK ainda
    /// poderia ler os interesses do aparelho. A ATTRIBUTION (medir conversao sem perfil) fica.
    /// Como: depois que o Unity gera o projeto Gradle, acrescenta ao manifesto do launcher (o de maior prioridade no merge)
    /// um uses-permission com tools:node="remove". Idempotente. O release-check do verify.sh reprova o AAB que ainda a tiver.
    /// </summary>
    public class ManifestPrivacidade : IPostGenerateGradleAndroidProject
    {
        public const string Topics = "android.permission.ACCESS_ADSERVICES_TOPICS";
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string unityLibrary)
        {
            // Unity 6: o callback recebe a pasta unityLibrary; o launcher fica ao lado. Sem launcher (export so da biblioteca), vai na unityLibrary.
            var launcher = Path.Combine(unityLibrary, "..", "launcher", "src", "main", "AndroidManifest.xml");
            var alvo = File.Exists(launcher) ? launcher : Path.Combine(unityLibrary, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(alvo)) { Debug.LogError("FS ManifestPrivacidade: manifesto nao achado em " + alvo); return; }
            Debug.Log("FS ManifestPrivacidade: " + (ManifestXml.Remover(alvo, Topics) ? "remove " : "ja tinha remove ") + Topics + " em " + alvo);
        }
    }
}
#endif
