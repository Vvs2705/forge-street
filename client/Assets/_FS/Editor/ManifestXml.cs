using System.Xml;

namespace FS.EditorTools
{
    /// <summary>XML puro do manifesto Android (sem UnityEditor.Android, compila em qualquer alvo e tem teste EditMode). Usado pelo ManifestPrivacidade.</summary>
    public static class ManifestXml
    {
        public const string NsAndroid = "http://schemas.android.com/apk/res/android";
        public const string NsTools = "http://schemas.android.com/tools";

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
