using System.IO;
using System.Xml;
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
        const string NsAndroid = "http://schemas.android.com/apk/res/android";
        const string NsTools = "http://schemas.android.com/tools";

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string unityLibrary)
        {
            // Unity 6: o callback recebe a pasta unityLibrary; o launcher fica ao lado. Sem launcher (export so da biblioteca), vai na unityLibrary.
            var launcher = Path.Combine(unityLibrary, "..", "launcher", "src", "main", "AndroidManifest.xml");
            var alvo = File.Exists(launcher) ? launcher : Path.Combine(unityLibrary, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(alvo)) { Debug.LogError("FS ManifestPrivacidade: manifesto nao achado em " + alvo); return; }
            Debug.Log("FS ManifestPrivacidade: " + (Remover(alvo, Topics) ? "remove " : "ja tinha remove ") + Topics + " em " + alvo);
        }

        /// <summary>Acrescenta &lt;uses-permission android:name=perm tools:node="remove"/&gt;; false se ja estava.</summary>
        public static bool Remover(string manifesto, string perm)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(manifesto);
            var raiz = doc.DocumentElement;
            if (!raiz.HasAttribute("xmlns:tools"))
            {
                var ns = doc.CreateAttribute("xmlns", "tools", "http://www.w3.org/2000/xmlns/");
                ns.Value = NsTools;
                raiz.Attributes.Append(ns);
            }
            XmlElement alvo = null;
            foreach (XmlNode n in raiz.ChildNodes)
                if (n is XmlElement e && e.Name == "uses-permission" && e.GetAttribute("name", NsAndroid) == perm) { alvo = e; break; }
            if (alvo != null && alvo.GetAttribute("node", NsTools) == "remove") return false;
            if (alvo == null)
            {
                alvo = doc.CreateElement("uses-permission");
                Attr(doc, alvo, "android", "name", NsAndroid, perm);
                raiz.InsertBefore(alvo, raiz.FirstChild);
            }
            Attr(doc, alvo, "tools", "node", NsTools, "remove");
            doc.Save(manifesto);
            return true;
        }

        static void Attr(XmlDocument doc, XmlElement e, string prefixo, string nome, string ns, string valor)
        {
            var a = doc.CreateAttribute(prefixo, nome, ns);
            a.Value = valor;
            e.Attributes.SetNamedItem(a);
        }
    }
}
