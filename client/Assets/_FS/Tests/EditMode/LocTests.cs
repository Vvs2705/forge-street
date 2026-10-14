using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// DM §109: a tabela do Loc tem os 3 idiomas em toda chave, toda chave que o codigo pede existe, nenhum texto visivel ficou fixo na
    /// View e as larguras da HUD valem em pt-BR, en e es (dica &lt;= 26, nome de melhoria &lt;= 16, cartao, ouro &lt;= 8).
    /// </summary>
    public class LocTests
    {
        static readonly Idioma[] Todos = { Idioma.Pt, Idioma.En, Idioma.Es };
        static readonly Tuning T0 = new Tuning();

        [TearDown]
        public void VoltaAoPtBr() => Loc.Atual = Idioma.Pt;   // o resto dos testes (TextosTests) le em pt-BR

        [Test]
        public void Tabela_TresIdiomasSemVazio_FormatoValido()
        {
            var placeholder = new Regex(@"\{(\d+)\}");
            foreach (KeyValuePair<string, string[]> kv in Loc.Todas)
            {
                Assert.AreEqual(3, kv.Value.Length, kv.Key);
                foreach (string s in kv.Value)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s), kv.Key);
                    foreach (string forma in s.Split('|'))
                    {
                        Assert.DoesNotThrow(() => string.Format(forma, "a", "b", "c"), kv.Key + ": " + forma);
                        foreach (Match m in placeholder.Matches(forma)) Assert.LessOrEqual(int.Parse(m.Groups[1].Value), 2, kv.Key);
                    }
                }
                // plural: "singular|plural" em todo idioma (o en pode repetir: "ore|ore")
                if (kv.Key.StartsWith("item.", StringComparison.Ordinal))
                    foreach (string s in kv.Value) Assert.AreEqual(2, s.Split('|').Length, kv.Key + ": " + s);
            }
            // o -shotorder procura o comeco do aviso: o premio vai no fim
            foreach (Idioma i in Todos) StringAssert.EndsWith("{0}", Loc.Em(i, "aviso.entregue"));
        }

        /// <summary>As chaves montadas no codigo (por ID) existem para todo upgrade, item e estacao.</summary>
        [Test]
        public void ChavesPorId_ExistemParaTodoConteudo()
        {
            Assert.AreEqual(Enum.GetValues(typeof(Item)).Length, Textos.ItemId.Length, "Textos.ItemId: 1 por Item");
            Assert.AreEqual(new Sim().Stations.Count, Textos.EstacaoId.Length, "Textos.EstacaoId: 1 por estacao");
            var faltam = new List<string>();
            void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
            foreach (UpgradeDef d in Upgrades.All) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
            foreach (string id in Textos.ItemId) { Tem("item." + id); Tem("dica.balcao." + id); Tem("dica.pegue." + id); }
            foreach (string id in Textos.EstacaoId) Tem("est." + id);
            foreach (Faixa f in Enum.GetValues(typeof(Faixa))) Tem("priv.faixa." + (int)f);
            foreach (string g in new[] { "m", "f", "mp", "fp" }) { Tem("art.o." + g); Tem("art.de." + g); }
            foreach (AdPlacementDef d in T0.Placements) Tem("ads." + d.Id);   // E-PLAT-02: cada placement do Tuning (o anuncio simulado)
            foreach (RenomeDef d in RenomeDefs.Niveis) { Tem(d.NomeKey); if (d.Nivel > 0) Tem(d.NomeKey + ".libera"); }   // B-CORE-06: SeloRenome
            foreach (EventDef e in LiveOpsDefs.Eventos)   // D-CORE-03/04: nome do evento e cosmeticos da trilha; nome e cosmeticos free do Passe
            {
                Tem("ev." + e.Id);
                foreach (Marco m in e.Trilha) foreach (RewardDef r in m.Premios) if (r.Tipo == TipoPremio.Cosmetico) Tem("cos." + r.Ref);
            }
            foreach (PassDef p in LiveOpsDefs.Passes) { Tem("passe." + p.Id); foreach (RewardDef r in p.Free) if (r.Tipo == TipoPremio.Cosmetico) Tem("cos." + r.Ref); }
            foreach (ProductDef d in Compras.Catalogo) Tem("iap." + d.Id + ".nome");   // E-PLAT-04: PainelLoja (nome e uma linha por posse)
            foreach (string id in Compras.Posses) Tem("iap.posse." + id);
            Assert.IsEmpty(faltam);
        }

        static string ViewDir([CallerFilePath] string aqui = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(aqui), "../../Scripts/View"));

        /// <summary>Linhas de codigo (sem comentario) de cada .cs da View, menos o proprio Loc.cs.</summary>
        static IEnumerable<(string arq, int n, string cod)> Codigo()
        {
            string dir = ViewDir();
            if (!Directory.Exists(dir)) Assert.Ignore("fontes da View fora do alcance: " + dir);
            foreach (string f in Directory.GetFiles(dir, "*.cs"))
            {
                if (Path.GetFileName(f) == "Loc.cs") continue;
                string[] linhas = File.ReadAllLines(f);
                for (int i = 0; i < linhas.Length; i++) yield return (Path.GetFileName(f), i + 1, SemComentario(linhas[i]));
            }
        }

        static string SemComentario(string l)
        {
            bool str = false;
            for (int i = 0; i < l.Length; i++)
            {
                char c = l[i];
                if (str) { if (c == '\\') i++; else if (c == '"') str = false; }
                else if (c == '\'' && i + 2 < l.Length && l[i + 2] == '\'') i += 2;   // '"' nao abre string
                else if (c == '"') str = true;
                else if (c == '/' && i + 1 < l.Length && l[i + 1] == '/') return l.Substring(0, i);
            }
            return l;
        }

        static readonly Regex Literal = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
        static bool EChave(string s) => Loc.Tem(s) || (s.EndsWith(".", StringComparison.Ordinal) && Loc.Todas.Any(kv => kv.Key.StartsWith(s, StringComparison.Ordinal)));

        [Test]
        public void ChavesDoCodigo_ExistemNaTabela()
        {
            var chave = new Regex("Loc\\.(?:T|P)\\(\\s*\"([^\"]+)\"|Loc\\.Em\\([^,()]+,\\s*\"([^\"]+)\"");
            var faltam = new List<string>();
            int n = 0;
            foreach ((string arq, int linha, string cod) in Codigo())
                foreach (Match m in chave.Matches(cod))
                {
                    n++;
                    string k = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                    if (!EChave(k)) faltam.Add($"{arq}:{linha} {k}");
                }
            Assert.Greater(n, 40, "o regex achou poucas chaves: mudou o jeito de chamar o Loc?");
            Assert.IsEmpty(faltam);
        }

        /// <summary>
        /// Nada de texto fixo na View: (1) em linha que escreve na tela (.text =, Notice, ShowPanel, Float, NewButton, Paint, Abrir) todo
        /// literal com 2+ letras e' chave do Loc (fora som, sprite e nome de objeto); (2) em qualquer linha, literal com cara de frase (letra
        /// espaco letra, acento, ou termina em ! ?) so em log, erro de dev ou excecao. ponytail: heuristica; palavra solta sem acento fora
        /// dessas linhas passa (nome de objeto). Falso positivo? Ajuste o regex aqui, nao esconda o texto.
        /// </summary>
        [Test]
        public void View_SemTextoVisivelFixo()
        {
            var sink = new Regex(@"\.text\s*\+?=|\b(Notice|ShowPanel|Float|NewButton|Paint|Abrir)\(");
            var log = new Regex(@"Debug\.Log|\bLog\(|Emit\(|Falha\(|throw ");
            var nome = new Regex(@"(Sfx\.Play|Art\.\w+|GameObject)\(\s*$");   // som, sprite e nome de objeto: nao e' texto
            var letras = new Regex(@"\p{L}{2,}");
            var frase = new Regex(@"\p{L}\s+\p{L}|[\p{L}-[a-zA-Z]]|\p{L}[!?]$");
            var ruins = new List<string>();
            foreach ((string arq, int linha, string cod) in Codigo())
            {
                if (arq == "Captura.cs" || log.IsMatch(cod)) continue;   // Captura: ferramenta de dev dos criativos, so erro no log
                bool tela = sink.IsMatch(cod);
                foreach (Match m in Literal.Matches(cod))
                {
                    string s = m.Groups[1].Value;
                    if (m.Index > 0 && cod[m.Index - 1] == '$') s = Regex.Replace(s, @"\{[^}]*\}", "");   // $"{a}/{b}": so o texto fixo
                    if (EChave(s) || !letras.IsMatch(s) || nome.IsMatch(cod.Substring(0, m.Index))) continue;
                    if (tela || frase.IsMatch(s)) ruins.Add($"{arq}:{linha} \"{s}\"");
                }
            }
            Assert.IsEmpty(ruins, "texto visivel fora do Loc (Loc.cs)");
        }

        [Test]
        public void Larguras_NosTresIdiomas()
        {
            var s = new Sim();
            foreach (Idioma i in Todos)
            {
                Loc.Atual = i;
                var dicas = new List<string>();
                for (int u = 0; u < Upgrades.Count; u++)
                {
                    dicas.Add(Textos.Placa(u)); dicas.Add(Textos.Menu(u));
                    Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                }
                for (int it = 0; it < Textos.ItemId.Length; it++)
                    foreach (Hint h in new[] { Hint.ProductToCounter, Hint.PickProducts, Hint.ClientWaiting }) dicas.Add(Textos.Dica(h, it, s));
                for (int c = 0; c < s.Chests.Count; c++) dicas.Add(Textos.Dica(Hint.OpenChest, c, s));
                foreach (Hint h in new[] { Hint.GrabOre, Hint.OreToFurnace, Hint.PickIngots, Hint.IngotToCrafter }) dicas.Add(Textos.Dica(h, 0, s));
                foreach (string d in dicas)
                {
                    Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                    StringAssert.DoesNotContain("[", d, $"{i}: chave faltando");
                }
                for (int u = 0; u < Upgrades.Count; u++)
                {
                    if (!Upgrades.All[u].InMenu) continue;
                    string b = Textos.Beneficio(T0, u);
                    StringAssert.Contains("%", b, $"{i}: {b}");
                    Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");   // 2 linhas do cartao
                    Assert.LessOrEqual(Textos.AntesDepois(T0, u).Length, 16, $"{i}: {Textos.AntesDepois(T0, u)}");
                    int req = Upgrades.All[u].Requires;
                    bool oculto = req >= 0 && Upgrades.All[u].Meta && !Upgrades.All[req].Meta;   // B-CORE-03: o nivel II nunca aparece travado (Cartao.Oculto)
                    if (req >= 0 && !oculto) Assert.LessOrEqual(Textos.Precisa(req).Length, 18, $"{i}: {Textos.Precisa(req)}");   // 1 linha de 22 px no botao
                }
                // E-PLAT-02: avisos (1 linha da pilula da dica) com numero grande; chip "Trocar" (~100 px) e o "Cofre 2x" (meio cartao)
                foreach (string k in new[] { "aviso.cofre2x", "aviso.bonus", "aviso.supply" }) Assert.LessOrEqual(Loc.T(k, 99999).Length, Textos.DicaMax, $"{i}: {k}");
                Assert.LessOrEqual(Loc.T("aviso.troca").Length, Textos.DicaMax, i.ToString());
                Assert.LessOrEqual(Loc.T("enc.troca", 30).Length, 11, $"{i}: {Loc.T("enc.troca", 30)}");
                Assert.LessOrEqual(Loc.T("cofre.dobrar", 99999).Length, 15, $"{i}: {Loc.T("cofre.dobrar", 99999)}");
                for (long v = 1; v > 0 && v < long.MaxValue / 7; v *= 7) Assert.LessOrEqual(Textos.Ouro(v).Length, 8, $"{i}: {Textos.Ouro(v)}");
                foreach (RenomeDef d in RenomeDefs.Niveis)   // B-CORE-06: selo (1 linha ao lado da estrela), aviso (1 linha na faixa) e cartao (3 linhas)
                {
                    Assert.LessOrEqual(Textos.NomeRenome(d.Nivel).Length, Textos.RenomeMax, $"{i}: {Textos.NomeRenome(d.Nivel)}");
                    Assert.LessOrEqual(Textos.AvisoRenome(d.Nivel).Length, Textos.AvisoMax, $"{i}: {Textos.AvisoRenome(d.Nivel)}");
                    if (d.Nivel > 0) Assert.LessOrEqual(Loc.T("renome.libera", Textos.LiberaRenome(d.Nivel)).Length, 110, $"{i}: nivel {d.Nivel}");
                }
                Assert.LessOrEqual(Textos.Ouro(long.MaxValue).Length, 8, i.ToString());
                // D-CORE-03/04: chip do evento (<= ChipMax), avisos de evento e do Passe (1 linha na faixa)
                foreach (long seg in new[] { 59L, 86399L, 6 * 86400L }) Assert.LessOrEqual(Loc.T("liveops.chip", Textos.Qtd(99999), Textos.Tempo(seg)).Length, Textos.ChipMax, i.ToString());
                foreach (EventDef e in LiveOpsDefs.Eventos)
                {
                    Assert.LessOrEqual(Textos.NomeEvento(e).Length, Textos.EventoMax, $"{i}: {e.Id}");
                    Assert.LessOrEqual(Loc.T("aviso.evento", Textos.NomeEvento(e)).Length, Textos.AvisoMax, $"{i}: {e.Id}");
                }
                Assert.LessOrEqual(Loc.T("aviso.passe_ouro", Textos.Qtd(99999)).Length, Textos.DicaMax, i.ToString());
                Assert.LessOrEqual(Loc.T("aviso.evento_marco", 1000).Length, Textos.AvisoMax, i.ToString());
                // E-PLAT-04: avisos da loja (1 linha da faixa), LOJA (1/3 da fileira das Configuracoes), COMPRADO e RESTAURAR, nome e linhas do conteudo
                foreach (string k in new[] { "aviso.compra_ok", "aviso.compra_falhou" }) Assert.LessOrEqual(Loc.T(k).Length, Textos.DicaMax, $"{i}: {k}");
                Assert.LessOrEqual(Loc.T("aviso.restaurado", 99).Length, Textos.DicaMax, $"{i}: aviso.restaurado");
                foreach ((string k, int max) in new[] { ("cfg.loja", 8), ("loja.comprado", 10), ("loja.restaurar", 20) }) Assert.LessOrEqual(Loc.T(k).Length, max, $"{i}: {k}");
                foreach (ProductDef d in Compras.Catalogo)
                {
                    Assert.LessOrEqual(Loc.T("iap." + d.Id + ".nome").Length, 24, $"{i}: {d.Id}");
                    foreach (ItemCompra c in d.Conteudo) Assert.LessOrEqual(Loc.T("iap.posse." + c.Id).Length, 40, $"{i}: {c.Id}");
                }
            }
        }

        static string O(long v) => Textos.Ouro(v).Replace(' ', ' ');

        [Test]
        public void Numeros_DoIdioma()
        {
            Assert.AreEqual(("1,15 mi", "4 → 2,5 s", "Faltam 1,2 mi"), (O(1150000), Textos.AntesDepois(T0, (int)Upgrade.FurnaceSpeed1), O2(Textos.Falta(1200000))));
            Loc.Atual = Idioma.En;
            Assert.AreEqual(("1.15M", "9.22Qi", "4 → 2.5 s", "Need 1.2M", "Need 1"),
                (O(1150000), O(long.MaxValue), Textos.AntesDepois(T0, (int)Upgrade.FurnaceSpeed1), Textos.Falta(1200000), Textos.Falta(1)));
            Loc.Atual = Idioma.Es;
            Assert.AreEqual(("1,15 M", "5 B", "4 → 2,5 s", "Faltan 1,2 M", "Falta 1"),
                (O(1150000), O(5000000000), Textos.AntesDepois(T0, (int)Upgrade.FurnaceSpeed1), O2(Textos.Falta(1200000)), Textos.Falta(1)));
        }

        static string O2(string s) => s.Replace(' ', ' ');

        [Test]
        public void Frases_ExemplosNosTresIdiomas()
        {
            var s = new Sim();
            string[] Em(Idioma i)
            {
                Loc.Atual = i;
                return new[]
                {
                    Textos.Placa((int)Upgrade.Anvil2), Textos.Menu((int)Upgrade.PlayerCapacity), Textos.Dica(Hint.ClientWaiting, (int)Item.Sword, s),
                    Textos.Dica(Hint.OpenChest, 0, s), Textos.Venda(1, (int)Item.Shield), Textos.Venda(5, (int)Item.Shield),
                    Textos.Precisa((int)Upgrade.Counter5), Textos.Estacao(s.JewelShop.Index), Privacidade.Atual(Faixa.Adulto, true),
                    Textos.AvisoRenome(2),
                };
            }
            Assert.AreEqual(new[] { "Pise na placa da Bigorna 2", "Compre a Mochila", "Cliente quer espada!", "Abra o baú: 15 espadas!",
                "Venda 1 escudo", "Venda 5 escudos", "Precisa: Balcão 5", "Loja de joias",
                "Resposta atual: 18 ou mais, anúncios personalizados. A mudança vale por completo quando você abrir o jogo de novo.", "Renome 2: Oficina conhecida!" }, Em(Idioma.Pt));
            Assert.AreEqual(new[] { "Step on the Anvil 2 pad", "Buy Backpack in Upgrades", "Customer wants swords!", "Open the chest: 15 swords!",
                "Sell 1 shield", "Sell 5 shields", "Needs Counter 5", "Jewel Shop",
                "Current answer: 18 or older, personalized ads. The change fully applies when you reopen the game.", "Renown 2: Known Workshop!" }, Em(Idioma.En));
            Assert.AreEqual(new[] { "Pisa la placa del Yunque 2", "Compra la Mochila", "¡Piden espadas!", "Abre el baúl: ¡15 espadas!",
                "Vende 1 escudo", "Vende 5 escudos", "Requiere 5 puestos", "Tienda de joyas",
                "Respuesta actual: 18 o más, anuncios personalizados. El cambio se aplica por completo al volver a abrir el juego.", "¡Renombre 2: Taller conocido!" }, Em(Idioma.Es));
        }

        [Test]
        public void Idioma_FlagVencePreferenciaQueVenceAparelho()
        {
            Assert.AreEqual(Idioma.Pt, Loc.DoSistema("Portuguese"));
            Assert.AreEqual(Idioma.Es, Loc.DoSistema("Spanish"));
            Assert.AreEqual(Idioma.En, Loc.DoSistema("English"));
            Assert.AreEqual(Idioma.En, Loc.DoSistema("Japanese"), "outro idioma = en");
            Assert.AreEqual(Idioma.En, Loc.DoSistema("Unknown"));
            Assert.AreEqual(Idioma.Es, Loc.Escolher("es", "en", "Portuguese"), "-lang vence");
            Assert.AreEqual(Idioma.En, Loc.Escolher(null, "en", "Portuguese"), "Configuracoes vencem o aparelho");
            Assert.AreEqual(Idioma.Pt, Loc.Escolher(null, "", "Portuguese"));
            Assert.AreEqual(Idioma.Es, Loc.Escolher("lixo", "", "Spanish"), "flag invalida cai na preferencia e no aparelho");
            foreach (string c in new[] { "pt", "pt-BR", "PT_br", "en-US", "es-419" }) Assert.IsTrue(Loc.Ler(c, out _), c);
            foreach (string c in new[] { null, "", "p", "fr", "ptx", "english" }) Assert.IsFalse(Loc.Ler(c, out _), c ?? "null");
            foreach (Idioma i in Todos)
            {
                Assert.IsTrue(Loc.Ler(Loc.CodigoDe(i), out Idioma volta));
                Assert.AreEqual(i, volta, "o codigo gravado nas Configuracoes volta ao mesmo idioma");
            }
        }

        /// <summary>O hash da Privacidade e' do pt-BR, em qualquer idioma da sessao: trocar o idioma nao pede a resposta de novo.</summary>
        [Test]
        public void Privacidade_TextoTravadoEmPtBr()
        {
            string pt = Privacidade.Texto;
            Loc.Atual = Idioma.Es;
            Assert.AreEqual(pt, Privacidade.Texto);
            StringAssert.StartsWith("Qual é a sua idade?", pt);
            Assert.AreEqual("18 o más", Privacidade.NomeFaixa(Faixa.Adulto));
        }
    }
}
