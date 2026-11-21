using System.IO;
using System.Xml;
using FS.EditorTools;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// D-PRIV-2 (docs/PUBLICACAO_PLAY.md §9): o ManifestXml.Remover que o hook do build Android usa para tirar a API Topics do manifesto.
    /// Sem xmlns:tools, com a permissao ja declarada e rodando duas vezes (idempotente).
    /// </summary>
    public class ManifestXmlTests
    {
        const string Perm = "android.permission.ACCESS_ADSERVICES_TOPICS";
        string arq;

        [SetUp] public void Cria() { arq = Path.Combine(Path.GetTempPath(), "fs_manifest_" + System.Guid.NewGuid().ToString("N") + ".xml"); }
        [TearDown] public void Apaga() { if (File.Exists(arq)) File.Delete(arq); }

        void Escreve(string corpo) =>
            File.WriteAllText(arq, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android=\"" + ManifestXml.NsAndroid + "\"" + corpo + "</manifest>\n");

        XmlElement[] Perms(string perm)
        {
            var doc = new XmlDocument(); doc.Load(arq);
            var l = new System.Collections.Generic.List<XmlElement>();
            foreach (XmlNode n in doc.DocumentElement.ChildNodes)
                if (n is XmlElement e && e.Name == "uses-permission" && e.GetAttribute("name", ManifestXml.NsAndroid) == perm) l.Add(e);
            return l.ToArray();
        }

        [Test] public void SemXmlnsTools_DeclaraEAcrescentaRemove()
        {
            Escreve(">\n  <application />\n");
            Assert.IsTrue(ManifestXml.Remover(arq, Perm));
            var doc = new XmlDocument(); doc.Load(arq);
            Assert.AreEqual(ManifestXml.NsTools, doc.DocumentElement.GetAttribute("xmlns:tools"));
            var p = Perms(Perm);
            Assert.AreEqual(1, p.Length);
            Assert.AreEqual("remove", p[0].GetAttribute("node", ManifestXml.NsTools));
            Assert.AreEqual(1, doc.GetElementsByTagName("application").Count, "nao mexe no resto");
        }

        [Test] public void PermissaoJaDeclarada_ViraRemoveSemDuplicar()
        {
            Escreve(" xmlns:tools=\"" + ManifestXml.NsTools + "\">\n  <uses-permission android:name=\"" + Perm + "\" />\n  <uses-permission android:name=\"android.permission.INTERNET\" />\n");
            Assert.IsTrue(ManifestXml.Remover(arq, Perm));
            var p = Perms(Perm);
            Assert.AreEqual(1, p.Length);
            Assert.AreEqual("remove", p[0].GetAttribute("node", ManifestXml.NsTools));
            Assert.AreEqual("", Perms("android.permission.INTERNET")[0].GetAttribute("node", ManifestXml.NsTools), "as outras ficam");
        }

        [Test] public void Idempotente()
        {
            Escreve(">\n");
            Assert.IsTrue(ManifestXml.Remover(arq, Perm));
            var antes = File.ReadAllText(arq);
            Assert.IsFalse(ManifestXml.Remover(arq, Perm));
            Assert.AreEqual(antes, File.ReadAllText(arq));
            Assert.AreEqual(1, Perms(Perm).Length);
        }
    }
}
