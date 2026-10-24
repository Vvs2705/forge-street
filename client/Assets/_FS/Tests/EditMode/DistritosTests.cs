using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-01/02/04 (GDD §6, §11; CONTEUDO_V1 §1, §2.2): DistrictDef em dados com o Old Forge identico ao de antes (layout, save de 1
    /// distrito ao byte, save real do schema 3 sem perda), a Armorer's Row so de dados (kit, efeitos genericos, montagem, rebitadeira, couro,
    /// bot ate a producao completa), o Imperio (carteira, Charter 1x sem reset, abertura, renda de fundo online e offline com o teto de 2 h,
    /// save multi-distrito) e o validator do mapa (layout, caminho do ferreiro, Charter). A prova ao byte contra a main (econsim e save do bot
    /// em 5 perfis) esta no BALANCE.md §31. NUnit 3.5 do Unity: tipos exatos nos dois lados (long com L, float com f).
    /// </summary>
    public class DistritosTests
    {
        const float Dt = 1f / 30f;
        static readonly DistrictDef Row = Distritos.ArmorersRow;

        static Sim NovaRow() => new Sim(distrito: Row);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static Item I(Sim s, string id) => (Item)Items.IndexOf(id, s.ItemDefs);
        static Station E(Sim s, string id) => s.Est(id);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }

        /// <summary>Como o jogador chega a Row: Old Forge em producao completa (Mochila e Botas) a 2.400/min, o Charter dele (forcado: as condicoes
        /// tem teste proprio), a abertura paga e o foco na Row.</summary>
        static Imperio Chegada()
        {
            Imperio imp = Imperio.Chegada(Row);
            Assert.AreEqual((Row.Id, 2, 40.0, 0L), (imp.Ativo.Distrito.Id, imp.Abertos.Count, imp.Raiz.RateEma, imp.Ativo.Gold), "abre, foca e paga a abertura");
            Assert.IsTrue(imp.Raiz.ProductionComplete);
            return imp;
        }

        static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        static DistrictDef Copia(DistrictDef d) => new DistrictDef
        {
            Id = d.Id, Nome = d.Nome, Indice = d.Indice, Itens = d.Itens, Receitas = d.Receitas, Upgrades = d.Upgrades, Workers = d.Workers,
            Projetos = d.Projetos, Estacoes = d.Estacoes, Pads = d.Pads, Obstaculos = d.Obstaculos, Marcos = d.Marcos, Inicio = d.Inicio,
            Contratacao = d.Contratacao, Largura = d.Largura, Parede = d.Parede, EsteiraDe = d.EsteiraDe, EsteiraPara = d.EsteiraPara,
            Ajuste = d.Ajuste, Abertura = d.Abertura, Charter = d.Charter, FundoFator = d.FundoFator,
        };

        // Save real do schema 3 (main 2a3ffcf, bot F2P aos 25 min: qualidade, ajudantes com carga, VIP, encomenda, Renome e projeto)
        const string SaveReal25 = "v=3\nt=1500.022\ngold=858\n" +
            "ups=fole:67.03,bigorna2:115.93,ajudante1:176.71,escudos:229.55,mochila:298.22,ajudante2:370.8,esteira:418.75,fole_duplo:439.85,botas:519.56,ferramentas:629.77,vitrine:828.42,fornalha2:1159.17,ajudante3:1394.41,corredor:870.78,balcao5:864.24,balcao6:866.21,balcao7:866.24,balcao8:866.28,bigornas_nivel2:652.46,bigornas_nivel3:958.12,escudos_nivel2:721.25,escudos_nivel3:1258.98,ferramentas_nivel2:682.19,ferramentas_nivel3:1042.43\n" +
            "up=11111111111110010000000001111\nupp=\nst1=0,0,1,0.979,619\nst2=0,0,0,0,24\nst3=4,2,1,0.858,143\nst4=0,0,0,0,84\nst5=2,0,1,0.244,153\nst6=0,0,0,0,100\nst8=0,0,0,0,0\nst10=0,0,0,0,0\n" +
            "stocks=espada:10\nsolds=espada:215,escudo:152,ferramenta:100\nholds=\nwks=0:minerio:2;1:lingote:1;2:escudo:1\nstocksq=espada:3:0\nholdsq=\nwksq=;;\n" +
            "qst3=60,0,0,0\nqst4=25,0,0,0\nqst5=85,0,0,0\nqst6=35,0,0,0\nqst8=0,0,0,0\nqst10=0,0,0,0\ngolpe=0\nstock=10,0,0,0\nsold=215,152,100,0\nms=2,2,0,2\n" +
            "ema=6.88469\nsaved=1001500\nclaim=0\nm=22.7,467,8563,0,0,891,8,427.92,24\npx=5.24,8.21\nhold=0,0,0,0,0,0\nwk=0:2,0,0,0,0,0;1:0,1,0,0,0,0;2:0,0,0,1,0,0\n" +
            "vip=319.852,5,3,3,1,0,0\nvipc=0\nord=4,27,20,85,8,0,0\nbst=1,0,0\nrn=23,0\nbp=bp_mw_primeira_lamina:Milestone\nbpq=espada:29,escudo:20,ferramenta:22\n";

        // ------------------------------------------------------------------ C-CORE-01: Old Forge em dados, identico

        /// <summary>O layout que era constante no construtor do Sim saiu do DistrictDef sem mudar nada: estacoes na mesma ordem (indice do save),
        /// mesmas posicoes, bocas e quem abre; os 23 pads com as mesmas cadeias; ponto de contratacao, inicio e esteira.</summary>
        [Test]
        public void OldForge_LayoutEmDados_IgualAoConstrutorDeAntes()
        {
            var s = new Sim();
            Assert.AreSame(Distritos.OldForge, s.Distrito);
            Assert.AreSame(Upgrades.All, s.UpgradeDefs);
            CollectionAssert.AreEqual(new[] { "deposito", "fornalha", "fornalha2", "bigorna", "bigorna2", "escudos", "ferramentas", "balcao", "joalheria", "loja_joias", "bigorna_longa" },
                s.Stations.ConvertAll(x => x.Id));
            Assert.AreEqual(new[] { s.Deposit, s.FurnaceA, s.FurnaceB, s.AnvilA, s.AnvilB, s.ShieldBench, s.ToolBench, s.Counter, s.JewelBench, s.JewelShop, s.LongAnvil }, s.Stations.ToArray());
            Assert.AreEqual(new[] { -1, -1, (int)Upgrade.Furnace2, -1, (int)Upgrade.Anvil2, (int)Upgrade.Shields, (int)Upgrade.Tools, -1, (int)Upgrade.Jewelry, (int)Upgrade.Jewelry, (int)Upgrade.LongAnvil },
                s.Stations.Select(x => x.UnlockBy).ToArray());
            Assert.AreEqual("1.50,1.50 1.50,2.15|1.50,5.50 0.60,5.50 2.40,5.50|1.50,9.50 0.60,9.50 2.40,9.50|4.50,13.00 4.50,12.35|12.00,6.50 11.10,6.50 12.90,6.50|" +
                            "12.00,11.50 12.00,10.85|5.00,1.50 4.10,1.50 5.90,1.50",
                string.Join("|", new[] { s.Deposit, s.FurnaceA, s.AnvilA, s.Counter, s.JewelBench, s.JewelShop, s.LongAnvil }.Select(x => x.Pos + " " + x.InAt + (x.Produces ? " " + x.OutAt : ""))),
                "posicoes e bocas (entrada; saida de quem produz)");
            Upgrade[][] cadeias =
            {
                new[] { Upgrade.FurnaceSpeed1, Upgrade.FurnaceSpeed2 }, new[] { Upgrade.Furnace2 }, new[] { Upgrade.Anvil2 }, new[] { Upgrade.Shields }, new[] { Upgrade.Tools },
                new[] { Upgrade.Helper1, Upgrade.Helper2, Upgrade.Helper3 }, new[] { Upgrade.HelperSpeed }, new[] { Upgrade.PlayerCapacity }, new[] { Upgrade.PlayerSpeed },
                new[] { Upgrade.Conveyor }, new[] { Upgrade.HammerSpeed }, new[] { Upgrade.CounterCapacity }, new[] { Upgrade.SideCorridor }, new[] { Upgrade.Jewelry },
                new[] { Upgrade.Jeweler }, new[] { Upgrade.JewelSpeed }, new[] { Upgrade.JewelVitrine }, new[] { Upgrade.WorkshopFacade }, new[] { Upgrade.WorkshopFloor },
                new[] { Upgrade.JewelryDecor }, new[] { Upgrade.Miner }, new[] { Upgrade.Jeweler2 }, new[] { Upgrade.LongAnvil },
            };
            Assert.AreEqual(cadeias.Length, s.Pads.Count);
            for (int i = 0; i < cadeias.Length; i++) CollectionAssert.AreEqual(cadeias[i], s.Pads[i].Chain, "pad " + i);
            Assert.AreEqual("0.50,6.50 8.30,1.50 5.00,1.50", s.Pads[0].Pos + " " + s.Pads[5].Pos + " " + s.Pads[22].Pos);
            Assert.AreEqual((Sim.HireSpot.ToString(), "4.50,3.50", Balance.WorkshopW - 0.3f), (s.Contratacao.ToString(), s.Player.Pos.ToString(), s.MaxX));
            Assert.AreSame(s.FurnaceA, s.EsteiraDe); Assert.AreSame(s.AnvilA, s.EsteiraPara);
            Assert.AreEqual(Balance.Milestones.Length, s.Chests.Count);
            Assert.AreEqual(9, s.Solids.Count, "deposito, fornalha, bigorna e os 2 balcoes + carroca e os 3 trechos da parede (a decoracao so com o luxo)");
        }

        /// <summary>Imperio de 1 distrito grava o texto do Sim.Save ao byte, e le o save de hoje como 1 distrito igual ao Sim.Load.</summary>
        [Test]
        public void OldForge_ImperioDeUmDistrito_SaveIgualAoByte()
        {
            var s = new Sim(); var b = new Bot();
            for (float t = 0f; t < 900f; t += Dt) b.Step(s, Dt);
            string sim = s.Save(5000);
            var imp = new Imperio(s);
            Assert.AreEqual(sim, imp.Save(5000));
            Assert.AreEqual(Sim.Load(sim).Save(6000), Imperio.Load(sim).Save(6000));
        }

        /// <summary>Save real do schema 3 (sem nenhuma chave do Imperio) vira um Imperio de 1 distrito, o Old Forge ativo, sem perder nada.</summary>
        [Test]
        public void SaveReal_Schema3_ViraOldForgeSemPerda()
        {
            Imperio imp = Imperio.Load(SaveReal25);
            Assert.AreEqual((1, Distritos.OldForgeId, 0), (imp.Abertos.Count, imp.Ativo.Distrito.Id, imp.Charters.Count));
            Assert.AreSame(imp.Raiz, imp.Ativo);
            Assert.AreEqual((858L, 858L, 24, 215), (imp.Saldo(Imperio.Ouro), imp.Ativo.Gold, imp.Ativo.UpgradesBought, imp.Ativo.SoldItems[(int)Item.Sword]));
            Assert.AreEqual(Sim.Load(SaveReal25).Save(1001600), imp.Save(1001600), "o payload que volta e' o do Sim.Load, ao byte");
            Assert.AreEqual(0, imp.Ativo.UnknownIds);
        }

        // ------------------------------------------------------------------ C-CORE-01: Armorer's Row so de dados

        [Test]
        public void Row_Kit_BalcaoDe6EOsNumerosDoDistrito()
        {
            Sim s = NovaRow();
            CollectionAssert.AreEqual(new[] { "deposito", "fornalha", "prensa", "rebitadeira", "montagem_elmo", "balcao" }, s.Stations.FindAll(x => x.Unlocked).ConvertAll(x => x.Id));
            Assert.AreEqual((6, 10, Balance.WorldW - 0.3f, true, false), (s.QueueCap, s.CounterCap, s.MaxX, s.QualityRevealed, s.HasConveyor));
            // row-pendencias (BALANCE.md §39): custos do §5.2 x 1,1 e precos do §3 x 2,4 (eram x 1,55 e x 3,5)
            Assert.AreEqual((530L, 46000L, 18), (s.T.Cost(0), s.T.Cost(U(s, "rebitadeira2")), Enumerable.Range(0, s.UpgradeDefs.Length).Count(s.IsProduction)), "C-CORE-05: os 18 do §5.2 (a fornalha de liga)");
            Assert.AreEqual((335, 550, 0), (s.PriceOf(I(s, "elmo")), s.PriceOf(I(s, "peitoral")), s.T.Price[(int)I(s, "placa")]));
            Assert.IsNull(s.JewelShop); Assert.IsNull(s.AnvilA);
            Assert.AreEqual(0, s.Workers.Count, "nenhum ajudante no kit");
            Assert.AreEqual(("montagem", "forja_armas", "carroca", "prensa"), (E(s, "montagem_elmo").Arte, E(s, "forja_hastes").Arte, E(s, "carroca_couro").Arte, E(s, "prensa").Arte),
                "StationDef.Arte chega ao Station (a View desenha por ela; null = pelo tipo e produto)");
            Assert.IsTrue(Tuning.Padrao.CustoTabela == null, "o Ajuste da Row nao vaza para o Tuning de outro Sim");
        }

        /// <summary>Efeito generico (UpgradeDef.Efeito): o Fole de couro e o Fole duplo agem nas fornalhas do distrito (prensa e rebitadeira
        /// tambem sao fornalhas), os Gabaritos nas montagens e forjas (x0,6, o Martelo veloz), o Trilho de placas e' a esteira prensa -> elmos.</summary>
        [Test]
        public void Row_EfeitosGenericos_FoleGabaritosTrilho()
        {
            Sim s = NovaRow();
            Assert.AreEqual((4f, 3f), (E(s, "fornalha").Time, E(s, "prensa").Time));
            Assert.AreEqual(6f, E(s, "montagem_elmo").Time, 1e-5f);
            Compra(s, "fole_couro");
            Assert.AreEqual((2.5f, 1.875f), (E(s, "fornalha").Time, E(s, "prensa").Time));
            Compra(s, "fole_couro_duplo", "gabaritos");
            Assert.AreEqual(1.6f, E(s, "fornalha").Time, 1e-5f);
            Assert.AreEqual(3.6f, E(s, "montagem_elmo").Time, 1e-5f);
            Compra(s, "trilho_placas");
            Assert.IsTrue(s.HasConveyor);
            Station p = E(s, "prensa"), m = E(s, "montagem_elmo");
            p.Out = 2;
            for (float t = 0f; t < 2.05f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, 2), (p.Out, m.Ins[m.InIndex(I(s, "placa"))]), "2 placas pela esteira em 2 s");
        }

        /// <summary>Montagem (CONTEUDO_V1 §2.2 regra 1, teste negativo): a peca so comeca com TODOS os componentes na quantidade da receita e
        /// consome tudo no inicio; a entrada guarda 2x o maior componente (peitoral: 8 rebites).</summary>
        [Test]
        public void Row_Montagem_SoComecaComTodosOsComponentes()
        {
            Sim s = NovaRow();
            Station m = E(s, "montagem_elmo");
            int placa = m.InIndex(I(s, "placa")), rebite = m.InIndex(I(s, "rebite"));
            m.Ins[placa] = 2; m.Ins[rebite] = 1;
            s.Tick(Dt, 0f, 0f);
            Assert.IsFalse(m.Busy, "falta 1 rebite: nao comeca");
            Assert.AreEqual((2, 1), (m.Ins[placa], m.Ins[rebite]), "e nao consome nada");
            m.Ins[rebite] = 2;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(m.Busy);
            Assert.AreEqual((0, 0), (m.Ins[placa], m.Ins[rebite]));
            Compra(s, "carroca_curtidor", "montagem_manoplas", "montagem_peitoral");
            Assert.AreEqual((4, 8), (m.InCap, E(s, "montagem_peitoral").InCap));
        }

        /// <summary>Rebitadeira (outputCount 4): 1 lingote -> 4 rebites de uma vez, em 32 s (row-pendencias, BALANCE.md §39: 8 x os 4 s do
        /// catalogo, para a Rebitadeira 2 fazer falta); a saida guarda 2 ciclos (8) e o ciclo so comeca se os 4 cabem (senao trava, como saida
        /// cheia).</summary>
        [Test]
        public void Row_Rebitadeira_QuatroRebitesPorLingote()
        {
            Sim s = NovaRow();
            Station r = E(s, "rebitadeira");
            Assert.AreEqual((6, 8, 32f), (r.InCap, r.OutCap, r.Time));
            r.In = 1;
            for (float t = 0f; t < 31.9f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, 0), (r.Out, r.Crafted), "32 s, nao 4");
            for (float t = 0f; t < 0.2f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((4, 1, 4), (r.Out, r.Crafted, s.Crafted[(int)I(s, "rebite")]));
            r.Out = 5; r.In = 1;
            for (float t = 0f; t < 32.5f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((5, 1, true, 1), (r.Out, r.Crafted, r.Blocked, r.In), "5 + 4 > 8: trava sem consumir o lingote");
            r.Out = 4;
            for (float t = 0f; t < 32.1f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((8, 2, 0), (r.Out, r.Crafted, r.In));
        }

        /// <summary>Carroca do curtidor: deposito de couro; o papel 0 (Ferraz 1) leva o bruto de qualquer deposito, o papel 1 (Ferraz 2) os
        /// intermediarios (lingote, placa, rebite) e nunca bruto nem produto; o ferreiro pega couro na boca da carroca.</summary>
        [Test]
        public void Row_DepositoDeCouro_EPapeisPorItem()
        {
            Sim s = NovaRow();
            Compra(s, "carroca_curtidor", "ferraz1", "ferraz2");
            Item couro = I(s, "couro");
            Station c = E(s, "carroca_couro");
            Assert.AreEqual((true, couro), (c.Unlocked, c.OutItem));
            Carrier p0 = s.Workers[0], p1 = s.Workers[1];
            Assert.AreEqual((0, 1), (p0.Role, p1.Role));
            Assert.AreEqual((true, true, false, false), (s.CanPick(p0, couro), s.CanPick(p0, Item.Ore), s.CanPick(p0, Item.Ingot), s.CanPick(p0, I(s, "elmo"))));
            Assert.AreEqual((true, true, true, false, false), (s.CanPick(p1, Item.Ingot), s.CanPick(p1, I(s, "placa")), s.CanPick(p1, I(s, "rebite")), s.CanPick(p1, couro), s.CanPick(p1, I(s, "elmo"))));
            s.Player.Pos = c.OutAt;
            for (float t = 0f; t < 1.3f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(3, s.Player.Held[(int)couro], "3 couros (mao sem Mochila)");
        }

        /// <summary>O handle 5 (a joia no Old Forge) e' a lanca na Row: sem loja de joias, vai ao balcao principal e a fila dele.</summary>
        [Test]
        public void Row_Handle5_VendeNoBalcaoPrincipal()
        {
            Sim s = NovaRow();
            Item lanca = I(s, "lanca");
            Assert.AreEqual((Item)5, lanca);
            Assert.AreEqual((true, true), (s.Sells(s.Counter, lanca), s.CounterFor(lanca) == s.Counter));
            Assert.AreSame(s.Queue, s.QueueFor(lanca));
        }

        /// <summary>Mochila e Botas sao do ferreiro (CONTEUDO_V1 §15 #15): no distrito valem as do Old Forge (Heroi); o resto do Old Forge nao.</summary>
        [Test]
        public void Row_Heroi_MochilaEBotasDoOldForge()
        {
            Sim s = NovaRow();
            Assert.AreEqual((3, 3f), (s.Player.Cap, s.Player.Speed));
            var raiz = new Sim();
            raiz.Buy(Upgrade.PlayerCapacity); raiz.Buy(Upgrade.PlayerSpeed); raiz.Buy(Upgrade.CounterCapacity); raiz.Buy(Upgrade.HelperSpeed);
            s.Heroi = raiz; s.Recompute();
            Assert.AreEqual((6, 4.2f, 10, false), (s.Player.Cap, s.Player.Speed, s.CounterCap, s.Has(Upgrade.HelperSpeed)), "Vitrine e Ajudantes ageis ficam no Old Forge");
        }

        /// <summary>Criterio do C-CORE-01: um distrito so de dados roda ate o fim. O bot F2P, chegando como o jogador, fecha a producao completa
        /// da Row, vende os 7 produtos e o fundo do Old Forge entra a 25% da taxa dele (600/min), fora do GoldEarned.</summary>
        [Test]
        public void Row_BotF2P_ChegaAProducaoCompleta()
        {
            Imperio imp = Chegada();
            Sim s = imp.Ativo; var b = new Bot();
            float t = 0f;
            for (; t < 150f * 60f && !s.ProductionComplete; t += Dt) { b.Step(s, Dt); imp.Fundo(Dt); }
            Assert.IsTrue(s.ProductionComplete, "producao completa em 150 min");
            for (int i = 0; i < s.ItemDefs.Length; i++) if (s.IsProduct((Item)i)) Assert.Greater(s.SoldItems[i], 0, s.ItemDefs[i].Id + " vendido");
            Assert.AreEqual(600.0 * t / 60.0, imp.FundoGanho, 600.0 * t / 60.0 * 0.01, "fundo = 10/s");
            Assert.AreEqual(0L, imp.Raiz.GoldEarned, "o fundo nao entra no GoldEarned");
        }

        // ------------------------------------------------------------------ C-CORE-02: Imperio

        /// <summary>V15: Charter do Old Forge = producao completa + Renome 400 + 1 Masterwork; 1x (o 2o recusa), sem reset (nada do distrito muda),
        /// +100 de Renome uma vez, e libera a Row para abrir com 140.000 de ouro (abrir de novo recusa).</summary>
        [Test]
        public void Charter_CondicoesUmaVezSemReset_LiberaAAbertura()
        {
            var raiz = new Sim(); var imp = new Imperio(raiz);
            Assert.AreEqual("producao", imp.CharterFalta(Distritos.OldForgeId));
            for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsProduction(i)) raiz.Buy((Upgrade)i);
            Assert.AreEqual("renome", imp.CharterFalta(Distritos.OldForgeId));
            long pede = Distritos.OldForge.Charter.Renome;   // BALANCE §48: 700, dentro do nivel 4
            raiz.Renome.Ganha(pede - 1);
            Assert.AreEqual("renome", imp.CharterFalta(Distritos.OldForgeId), "o Renome do Charter, nao o limiar do nivel");
            raiz.Renome.Ganha(1);
            Assert.AreEqual("masterwork", imp.CharterFalta(Distritos.OldForgeId));
            raiz.Colecao.Superiores[(int)Item.Sword] = Distritos.OldForge.Charter.Superiores - 1;
            Assert.AreEqual("masterwork", imp.CharterFalta(Distritos.OldForgeId), "uma Superior a menos");
            raiz.Colecao.Superiores[(int)Item.Sword]++;
            Assert.IsNull(imp.CharterFalta(Distritos.OldForgeId), "BALANCE §48: as Superiores no lugar da obra-prima");
            raiz.Colecao.Superiores[(int)Item.Sword] = 0;
            Assert.AreEqual("masterwork", imp.CharterFalta(Distritos.OldForgeId));
            raiz.Masterworks = 1;
            Assert.IsNull(imp.CharterFalta(Distritos.OldForgeId));
            Assert.IsFalse(imp.Liberado(Row.Id));
            Assert.IsFalse(imp.Abrir(Row.Id), "sem Charter nao abre");
            int compras = raiz.UpgradesBought; long gold = raiz.Gold;
            Assert.IsTrue(imp.Charter(Distritos.OldForgeId));
            Assert.AreEqual((pede + 100L, compras, gold), (raiz.Renome.Total, raiz.UpgradesBought, raiz.Gold), "+100 de Renome, sem reset");
            Assert.IsFalse(imp.Charter(Distritos.OldForgeId));
            Assert.AreEqual(("concedido", pede + 100L), (imp.CharterFalta(Distritos.OldForgeId), raiz.Renome.Total));
            Assert.IsTrue(imp.Liberado(Row.Id));
            raiz.Gold = Row.Abertura - 1;
            Assert.IsFalse(imp.Abrir(Row.Id), "sem ouro");
            raiz.Gold = Row.Abertura + 7;
            Assert.IsTrue(RemoteConfig.Aplicar(raiz.T, "{\"DistritosNovos\": false}", false).Valido);
            Assert.IsFalse(imp.Abrir(Row.Id), "DM §130: a flag do RemoteConfig desliga distrito novo sem build");
            raiz.T.DistritosNovos = true;
            Assert.IsTrue(imp.Abrir(Row.Id));
            Assert.AreEqual((7L, 2, true), (raiz.Gold, imp.Abertos.Count, imp.Aberto(Row.Id)));
            Assert.IsFalse(imp.Abrir(Row.Id), "ja aberto");
            Assert.AreEqual("fechado", imp.CharterFalta("distrito_que_nao_existe"));
        }

        /// <summary>V9: carteira por ID de moeda, so ouro; trocar de distrito leva o saldo inteiro; ouro que cai num distrito fora de foco
        /// (meta da raiz) vai para a carteira.</summary>
        [Test]
        public void Carteira_TrocaLevaOSaldoERecolheOuroDeFora()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo, raiz = imp.Raiz;
            row.Gold = 1000;
            Assert.AreEqual((1000L, 0L), (imp.Saldo(Imperio.Ouro), imp.Saldo("gems")));
            raiz.Gold += 50;   // ex.: meta paga pelo Metas da raiz
            imp.Fundo(0f);
            Assert.AreEqual((1050L, 0L), (row.Gold, raiz.Gold));
            Assert.IsTrue(imp.Trocar(Distritos.OldForgeId));
            Assert.AreEqual((1050L, 0L, Distritos.OldForgeId), (raiz.Gold, row.Gold, imp.Ativo.Distrito.Id));
            Assert.IsFalse(imp.Trocar(Distritos.OldForgeId), "ja ativo");
            Assert.IsFalse(imp.Trocar("fechado"));
        }

        /// <summary>Renda de fundo (V25): distrito com Charter e fora de foco rende FundoFator (25%) x a RateEma congelada dele, sempre menos que
        /// a ativa; fora do GoldEarned; o ativo e distrito sem Charter nao rendem fundo.</summary>
        [Test]
        public void Fundo_25PorCentoDaTaxa_SoComCharterEForaDeFoco()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo, raiz = imp.Raiz;
            row.Gold = 0;
            for (int i = 0; i < 60; i++) imp.Fundo(1f);
            Assert.AreEqual((600L, 600L, 0L, 0L), (row.Gold, imp.FundoGanho, row.GoldEarned, raiz.GoldEarned));
            Assert.AreEqual((0.0, 10.0), (imp.FundoPorSegundo(row), imp.FundoPorSegundo(raiz)));
            Assert.Less(imp.FundoPorSegundo(raiz), raiz.RateEma, "fundo < ativa");
            imp.Charters.Remove(Distritos.OldForgeId);
            Assert.AreEqual(0.0, imp.FundoPorSegundo(raiz), "sem o Charter (sem manager) nao ha fundo");
            imp.Charters.Add(Distritos.OldForgeId);
            row.RateEma = 100.0;
            imp.Trocar(Distritos.OldForgeId);
            Assert.AreEqual((0.0, 0.0), (imp.FundoPorSegundo(row), imp.FundoPorSegundo(raiz)), "a Row ainda sem Charter; o Old Forge agora ativo");
        }

        /// <summary>P2: o fundo offline e' a mesma taxa com o teto de 2 h do nucleo (10 h fora = 7.200 s), 1x por claimId; Δt &lt;= 0 = 0.</summary>
        [Test]
        public void Fundo_Offline_TetoDeDuasHorasEIdempotente()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo;
            row.Gold = 0;
            Assert.AreEqual(0L, imp.ApplyOffline(-50, 10));
            Assert.AreEqual(72000L, imp.ApplyOffline(36000, 11), "10/s x 7.200 s (a Row nao vendeu: o cofre dela e' 0)");
            Assert.AreEqual(0L, imp.ApplyOffline(36000, 11), "mesmo claim");
            Assert.AreEqual(0L, imp.ApplyOffline(36000, 9), "claim antigo");
            Assert.AreEqual((72000L, 0L), (row.Gold, row.OfflineEarned));
            imp.Raiz.T.OfflineMaxSeconds = 999999;
            Assert.AreEqual(72000L, imp.ApplyOffline(36000, 12), "o knob nao passa das 2 h");
        }

        /// <summary>Save multi-distrito: payload da raiz (com a carteira no gold=) + imp=/charter=/dist.&lt;id&gt;= (base64); volta com o
        /// mesmo ativo, saldo, compras e pilhas, e regrava igual. Distrito de build mais novo volta no save; base64 ruim fecha so ele.</summary>
        [Test]
        public void Save_MultiDistrito_IdaEVolta()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo; var b = new Bot();
            for (float t = 0f; t < 240f; t += Dt) { b.Step(row, Dt); imp.Fundo(Dt); }
            long w = row.Gold;
            string text = imp.Save(9000);
            StringAssert.Contains("\nimp=armorers_row\n", text);
            StringAssert.Contains("\ncharter=old_forge\n", text);
            StringAssert.Contains("\ndist.armorers_row=", text);
            StringAssert.StartsWith("v=3\n", text);
            StringAssert.Contains("\ngold=" + w + "\n", text, "a carteira no gold= da raiz");
            Assert.AreEqual((w, 0L), (row.Gold, imp.Raiz.Gold), "salvar nao mexe na carteira");
            Imperio back = Imperio.Load(text);
            Sim r2 = back.Ativo;
            Assert.AreEqual((Row.Id, w, 0L, row.UpgradesBought), (r2.Distrito.Id, r2.Gold, back.Raiz.Gold, r2.UpgradesBought));
            Assert.AreSame(back.Raiz, r2.Heroi);
            CollectionAssert.AreEqual(row.Stations.ConvertAll(x => x.Out), r2.Stations.ConvertAll(x => x.Out));
            CollectionAssert.AreEqual(row.Stock, r2.Stock);
            Assert.IsTrue(back.Charters.Contains(Distritos.OldForgeId));
            string again = back.Save(9000);   // (a encomenda entregue zera os campos velhos no load, como no Old Forge)
            Assert.AreEqual(again, Imperio.Load(again).Save(9000), "regrava igual");

            Imperio futuro = Imperio.Load(text + "dist.distrito_novo=QUJD\n");
            Assert.AreEqual(2, futuro.Abertos.Count);
            StringAssert.Contains("\ndist.distrito_novo=QUJD\n", futuro.Save(9000), "distrito de build mais novo volta no save");
            Imperio ruim = Imperio.Load(text.Replace("dist.armorers_row=", "dist.armorers_row=%%"));
            Assert.AreEqual((1, Distritos.OldForgeId, w), (ruim.Abertos.Count, ruim.Ativo.Distrito.Id, ruim.Ativo.Gold), "base64 ruim: a raiz e a carteira ficam");
        }

        // ------------------------------------------------------------------ textos (pt/en/es) do distrito

        /// <summary>Toda chave montada por ID existe para a Row (melhoria, item, estacao, distrito) e cabe nos 3 idiomas: nome da melhoria <= 16,
        /// dica <= 26, cartao do menu com % em 2 linhas e antes/depois <= 16. O contexto do Textos volta ao Old Forge no fim.</summary>
        [Test]
        public void Loc_ChavesELarguras_DaRow()
        {
            Sim s = NovaRow();
            try
            {
                Textos.Distrito(Row);
                var faltam = new List<string>();
                void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
                foreach (UpgradeDef d in Row.Upgrades) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
                foreach (ItemDef d in Row.Itens) { Tem("item." + d.Id); Tem("dica.balcao." + d.Id); Tem("dica.pegue." + d.Id); }
                foreach (StationDef d in Row.Estacoes) Tem("est." + d.Id);
                foreach (DistrictDef d in Distritos.All) Tem("distrito." + d.Id);
                CollectionAssert.IsEmpty(faltam);
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int u = 0; u < Row.Upgrades.Length; u++)
                    {
                        Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                        foreach (string d in new[] { Textos.Placa(u), Textos.Menu(u) }) Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                        if (!Row.Upgrades[u].InMenu) continue;
                        string b = Textos.Beneficio(s.T, u);
                        StringAssert.Contains("%", b, $"{i}: {b}");
                        Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");
                        Assert.LessOrEqual(Textos.AntesDepois(s.T, u).Length, 16, $"{i}: {Textos.AntesDepois(s.T, u)}");
                    }
                    for (int it = 0; it < Row.Itens.Length; it++)
                        foreach (Hint h in new[] { Hint.ProductToCounter, Hint.PickProducts, Hint.ClientWaiting })
                        {
                            string d = Textos.Dica(h, it, s);
                            Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                            StringAssert.DoesNotContain("[", d, $"{i}: chave faltando");
                        }
                    Assert.AreEqual(Loc.T("est.montagem_elmo"), Textos.Estacao(E(s, "montagem_elmo").Index));
                }
                Assert.AreEqual("Couro e nova linha: grevas", Loc.Em(Idioma.Pt, "up." + Row.Upgrades[U(s, "carroca_curtidor")].Id + ".desc"));
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
            Assert.AreEqual("Pise na placa da Bigorna 2", Textos.Placa((int)Upgrade.Anvil2), "o Old Forge de volta");
        }

        /// <summary>Toda `arte:` de estacao tem a folha em Resources/Sprites (sem ela a View cai no desenho procedural sem avisar).</summary>
        [Test]
        public void Arte_TodaEstacaoTemAFolha()
        {
            string dir = SpritesDir();
            if (!System.IO.Directory.Exists(dir)) Assert.Ignore("sprites fora do alcance: " + dir);
            var faltam = Distritos.All.SelectMany(d => d.Estacoes.Where(e => e.Arte != null && !System.IO.File.Exists(System.IO.Path.Combine(dir, e.Arte, "Static.png")))
                .Select(e => d.Id + "/" + e.Id + " -> " + e.Arte)).ToList();
            Assert.IsEmpty(faltam);
        }

        /// <summary>Todo worker dos distritos tem a folha da skin (o Felix e a Vulcana com as proprias, ASSETS s20 e s23).</summary>
        [Test]
        public void Arte_TodoWorkerTemAFolha()
        {
            string dir = SpritesDir();
            if (!System.IO.Directory.Exists(dir)) Assert.Ignore("sprites fora do alcance: " + dir);
            var faltam = Distritos.All.SelectMany(d => (d.Workers ?? new WorkerDef[0]).Where(w => !System.IO.File.Exists(System.IO.Path.Combine(dir, w.Skin, "Idle.png")))
                .Select(w => d.Id + "/" + w.Id + " -> " + w.Skin)).ToList();
            Assert.IsEmpty(faltam);
        }

        static string SpritesDir([System.Runtime.CompilerServices.CallerFilePath] string aqui = "") =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(aqui), "../../Resources/Sprites"));

        // ------------------------------------------------------------------ C-CORE-04 / validator

        /// <summary>O mapa real passa (layout, caminho do ferreiro em todo distrito, Charter); a Row tambem nas regras de conteudo de sempre.</summary>
        [Test]
        public void Validator_MapaReal_SemViolacao()
        {
            CollectionAssert.IsEmpty(ContentValidator.Mapa(Distritos.All).ToList());
            // C-CORE-08: o mapa e' a cadeia dos Charters a partir do Old Forge (C-CORE-09: os 6 do build, a Grand Forge por ultimo)
            var cadeia = new List<string>();
            for (DistrictDef d = Distritos.OldForge; d != null; d = Distritos.Find(d.Charter?.Proximo)) cadeia.Add(d.Id);
            CollectionAssert.AreEqual(ContentValidator.Distritos, cadeia);
            CollectionAssert.AreEqual(new[] { "old_forge", "armorers_row", "artisan_quarter", "royal_arsenal", "arcane_foundry", "grand_forge" }, cadeia);
        }

        [Test]
        public void Validator_LayoutQuebrado_Acusa()
        {
            DistrictDef d = Copia(Row);
            d.Estacoes = Row.Estacoes.Where(s => s.Id != "balcao").Append(new StationDef("prensa", Kind.Furnace, 13f, 4f, "Prensa", -1f, 0f, "nao_existe", "prensa"))
                .Append(new StationDef("lixo", Kind.Deposit, 13f, 12f, "Lixo", 0f, 1f, null, null, (Item)2)).ToArray();
            d.Pads = Row.Pads.Append(new PadDef(12f, 4f, "upgrade_fantasma")).ToArray();
            var v = ContentValidator.Distrito(d).ToList();
            Acusa(v, "sem a estacao \"balcao\"");
            Acusa(v, "estacao \"prensa\" repetida");
            Acusa(v, "aberta por \"nao_existe\"");
            Acusa(v, "deposito lixo da o item 2, que nao e' bruto");
            Acusa(v, "vende \"upgrade_fantasma\"");

            d = Copia(Row);
            d.EsteiraPara = "bigorna_pesada";   // usa lingote e rebite; a prensa faz placa
            d.Inicio = new V2(1.5f, 4.0f);      // dentro da fornalha
            v = ContentValidator.Distrito(d).ToList();
            Acusa(v, "esteira prensa -> bigorna_pesada nao liga");
            Acusa(v, "inicio do ferreiro (1.50,4.00) fora do mapa ou dentro de um corpo");
        }

        /// <summary>C-CORE-04 minimo: um muro de ponta a ponta no meio da rua deixa as fornalhas e o resto de cima fora do alcance do ferreiro.</summary>
        [Test]
        public void Validator_EstacaoInalcancavel_Acusa()
        {
            DistrictDef d = Copia(Row);
            d.Obstaculos = new[] { new ObstacleDef("muro", 7.5f, 3.0f, 7.5f, 0.1f) };
            var v = ContentValidator.Alcancavel(d).ToList();
            Acusa(v, "entrada de fornalha (0.60,4.00) inalcancavel do inicio (kit");
            Acusa(v, "entrada de balcao");
            Assert.IsFalse(v.Exists(m => m.Contains("entrada de deposito")), "o deposito fica do lado do ferreiro");
        }

        [Test]
        public void Validator_CharterEMapa_Acusa()
        {
            DistrictDef d = Copia(Row);
            d.Charter = new CharterDef(true, 99999, -1, "nao_existe");
            d.FundoFator = 1.2f;
            var v = ContentValidator.Charter(d, Distritos.All).ToList();
            Acusa(v, "pede Renome 99999");
            Acusa(v, "fora do nivel 5 (o que libera charter_armorers_row: de 750 ate antes de 1250)");
            Acusa(v, "pede -1 Masterworks");
            Acusa(v, "libera \"nao_existe\"");
            Acusa(v, "renda de fundo 1.2");
            Acusa(ContentValidator.Charter(Copia(Row).Also(x => x.Charter = null), Distritos.All).ToList(), "Charter ausente");

            v = ContentValidator.Mapa(new[] { Distritos.OldForge, Row, Row }).ToList();
            Acusa(v, "distrito armorers_row: ID repetido");
            Acusa(v, "indice 1 na posicao 2");
            DistrictDef sem = Copia(Row); sem.Abertura = 0;
            Acusa(ContentValidator.Mapa(new[] { Distritos.OldForge, sem }).ToList(), "abertura 0");
        }
    }

    static class DistritosTestsExt
    {
        public static T Also<T>(this T x, Action<T> f) { f(x); return x; }
    }
}
