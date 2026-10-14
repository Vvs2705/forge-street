using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// E-PLAT-04/05/07 (GDD §17.3, §23 item 16; DM §93, §133): compras pelo nucleo com a loja falsa. Catalogo validado (nada aleatorio, sem
    /// moeda que nao esta a venda, sem assinatura); ordem entrega -> save GRAVADO -> confirmacao; ledger por ID de transacao (interrompida,
    /// duplicada, restaurada 2x e depois de reinstalar, recibo de outro produto/app/forjado, relogio para tras); posse so do recibo valido
    /// (save editado nao liga nada); Pacote do Mestre = veto do intersticial + Velocidade/Lingotes/Trocar sem video com as mesmas regras.
    /// </summary>
    public class ComprasTests
    {
        const string Pacote = "iap_pacote_mestre";
        const long T = 1000000;

        /// <summary>O que o Game faz: recibo -> Compras.Receber -> Compras.Concluir (grava no `disco` e so entao confirma a loja).</summary>
        sealed class Jogo
        {
            public Sim S; public LojaFalsa L; public string Disco; public int Gravacoes;
            public readonly List<EstadoCompra?> Estados = new List<EstadoCompra?>();
            public readonly List<string> Ordem = new List<string>(), Falhas = new List<string>();
            public bool SaveFalha;

            public Jogo(LojaFalsa loja, string disco = null)
            {
                L = loja; Disco = disco;
                S = disco == null ? new Sim() : Sim.Load(disco);
                S.Compras.Conectar(loja);
                L.Iniciar(Compras.Catalogo, Recibo, (p, m) => { Falhas.Add(p + ":" + m); S.Compras.Falhou(p); });
                Concluir();
            }

            public void Recibo(Recibo r) { Estados.Add(S.Compras.Receber(r)); Concluir(); }

            public List<Recibo> Concluir() => S.Compras.Concluir(() =>
            {
                Ordem.Add("gravar");
                if (SaveFalha) return false;
                Gravacoes++;
                Disco = S.Save(T);
                return true;
            }, tx => { Ordem.Add("confirmar"); L.Confirmar(tx); });

            public void Comprar(string id) { if (S.Compras.Iniciar(id)) L.Comprar(id); }
        }

        static string Temp() => Path.Combine(Path.GetTempPath(), "fs_lojafalsa_" + Guid.NewGuid().ToString("N") + ".txt");

        // ------------------------------------------------------------------ catalogo e validator

        [Test]
        public void Catalogo_PassaNoValidator_PacoteDoMestreNaoConsumivelSemMoeda()
        {
            Assert.IsEmpty(Compras.Validar(Compras.Catalogo).ToList());
            ProductDef d = Compras.Def(Pacote);
            Assert.IsNotNull(d, "E-PLAT-05");
            Assert.AreEqual(TipoProduto.NaoConsumivel, d.Tipo);
            CollectionAssert.AreEquivalent(new[] { Compras.RemoveAds, Compras.SemVideo }, d.Conteudo.Select(c => c.Id).ToArray(), "remove o intersticial + conveniencia");
            Assert.IsTrue(d.Conteudo.All(c => c.Tipo == TipoConteudo.Posse), "sem 2a moeda (V9)");
            Assert.IsNotEmpty(d.PrecoPlaceholder, "preco so de referencia, marcado");
            Assert.IsEmpty(Compras.MoedasAVenda, "V9: nenhuma moeda a venda ate a Wave E");
            CollectionAssert.AreEquivalent(new[] { Anuncio.Velocidade, Anuncio.EncomendaTroca, Anuncio.Supply }, Compras.SemVideoCobre, "V20: so as conveniencias");
            Assert.IsFalse(Compras.Posses.Any(x => x.Contains("pass")), "o pacote nunca liga o premium do Passe (produto proprio da Wave E)");
            Assert.IsFalse(LiveOpsDefs.PremiumLigado);
        }

        [Test]
        public void Validator_RecusaAleatorioMoedaQueNaoEstaAVendaOuroEAssinatura()
        {
            CollectionAssert.AreEqual(new[] { "Posse", "Moeda" }, Enum.GetNames(typeof(TipoConteudo)), "nada aleatorio e' estrutural: sem Bau, Chance, Aleatorio");
            ProductDef P(string id, TipoProduto t, params ItemCompra[] c) => new ProductDef { Id = id, Tipo = t, Conteudo = c, PrecoPlaceholder = "R$ 1" };
            ItemCompra M(string id, long q = 10) => new ItemCompra { Tipo = TipoConteudo.Moeda, Id = id, Qtd = q };
            var casos = new (ProductDef d, string erro)[]
            {
                (P("pacote", TipoProduto.NaoConsumivel, ItemCompra.Posse(Compras.RemoveAds)), "ID fora de iap_"),
                (P("iap_Pacote", TipoProduto.NaoConsumivel, ItemCompra.Posse(Compras.RemoveAds)), "ID fora de iap_"),
                (P("iap_bau", TipoProduto.NaoConsumivel, ItemCompra.Posse("bau_surpresa")), "desconhecida"),
                (P("iap_vazio", TipoProduto.NaoConsumivel), "sem conteudo"),
                (P("iap_ouro", TipoProduto.Consumivel, M("gold")), "nao esta a venda"),
                (P("iap_gems", TipoProduto.Consumivel, M("gems")), "nao esta a venda"),
                (P("iap_renome", TipoProduto.Consumivel, M("renome")), "nao esta a venda"),
                (P("iap_zero", TipoProduto.Consumivel, M("gold", 0)), "quantidade 0"),
                (P("iap_mensal", TipoProduto.Assinatura, ItemCompra.Posse(Compras.RemoveAds)), "assinatura"),
                (P("iap_moeda_fixa", TipoProduto.NaoConsumivel, M("gold")), "num nao-consumivel"),
                (P("iap_posse_gasta", TipoProduto.Consumivel, ItemCompra.Posse(Compras.SemVideo)), "num consumivel"),
                (P("iap_2x", TipoProduto.NaoConsumivel, ItemCompra.Posse(Compras.SemVideo), ItemCompra.Posse(Compras.SemVideo)), "repetido"),
                (new ProductDef { Id = "iap_sem_preco", Tipo = TipoProduto.NaoConsumivel, Conteudo = new[] { ItemCompra.Posse(Compras.RemoveAds) } }, "placeholder"),
            };
            foreach ((ProductDef d, string erro) in casos)
            {
                List<string> v = Compras.Validar(new[] { d }).ToList();
                Assert.IsTrue(v.Any(e => e.Contains(erro)), d.Id + ": esperava '" + erro + "', veio " + string.Join(" | ", v));
            }
            Assert.IsTrue(Compras.Validar(new[] { Compras.Catalogo[0], Compras.Catalogo[0] }).Any(e => e.Contains("ID repetido")));
        }

        // ------------------------------------------------------------------ fluxo (E-PLAT-04)

        [Test]
        public void CompraNormal_EntregaGravaEConfirma_NessaOrdem()
        {
            var j = new Jogo(new LojaFalsa(null));
            Assert.IsTrue(j.S.Compras.PodeComprar(Pacote));
            Assert.IsTrue(j.S.Compras.Iniciar(Pacote));
            Assert.AreEqual(Pacote, j.S.Compras.Iniciada, "iniciada");
            Assert.IsFalse(j.S.Compras.Iniciar(Pacote), "uma por vez");
            j.L.Comprar(Pacote);
            CollectionAssert.AreEqual(new EstadoCompra?[] { EstadoCompra.Pendente }, j.Estados, "pendente: entregue no estado, sem save ainda");
            CollectionAssert.AreEqual(new[] { "gravar", "confirmar" }, j.Ordem, "so confirma depois do save gravado");
            Assert.IsNull(j.S.Compras.Iniciada);
            Assert.IsTrue(j.S.Compras.Possui(Pacote));
            Assert.IsTrue(j.S.Compras.Tem(Compras.RemoveAds) && j.S.Anuncios.RemoveAds, "remove_ads chega ao veto do intersticial");
            Assert.AreEqual(0, j.L.NaoConfirmadas, "a loja sabe");
            Assert.IsFalse(j.S.Compras.PodeComprar(Pacote), "nao-consumivel: comprado");
            Sim b = Sim.Load(j.Disco);
            b.Compras.Conectar(j.L);
            Assert.IsTrue(b.Compras.Possui(Pacote), "o save gravado leva a compra");
        }

        [Test]
        public void SaveQueFalha_NadaConfirmado_ATentativaSeguinteConfirma()
        {
            var j = new Jogo(new LojaFalsa(null)) { SaveFalha = true };
            j.Comprar(Pacote);
            CollectionAssert.AreEqual(new[] { "gravar" }, j.Ordem, "save falhou: nada confirmado");
            Assert.IsTrue(j.S.Compras.Pendentes);
            Assert.AreEqual(1, j.L.NaoConfirmadas, "a loja reentrega se o app morrer agora");
            j.SaveFalha = false;
            Assert.AreEqual(1, j.Concluir().Count);
            CollectionAssert.AreEqual(new[] { "gravar", "gravar", "confirmar" }, j.Ordem);
            Assert.AreEqual(0, j.L.NaoConfirmadas);
        }

        [Test]
        public void Exploit_CompraInterrompida_AppMortoEntrePagarESalvar_ChegaNaProximaAbertura()
        {
            string conta = Temp();
            try
            {
                var j = new Jogo(new LojaFalsa(conta, LojaFalsa.Cenario.Interrompe));
                string antes = j.S.Save(T);
                j.Comprar(Pacote);
                Assert.IsEmpty(j.Estados, "pagou e o app morreu antes do recibo");
                Assert.IsFalse(j.S.Compras.Possui(Pacote));
                var k = new Jogo(new LojaFalsa(conta), antes);   // reabre: mesma conta da loja, o save de antes da compra
                CollectionAssert.AreEqual(new EstadoCompra?[] { EstadoCompra.Pendente }, k.Estados, "a loja reentrega o pago e nao confirmado");
                Assert.IsTrue(k.S.Compras.Possui(Pacote));
                Assert.AreEqual(1, k.S.Compras.Transacoes);
                Assert.AreEqual(0, new LojaFalsa(conta).NaoConfirmadas, "confirmada depois do save");
            }
            finally { File.Delete(conta); }
        }

        [Test]
        public void Exploit_AppMortoDepoisDoSave_AntesDaConfirmacao_ReabrirSoConfirma()
        {
            var loja = new LojaFalsa(null);
            Sim s = new Sim();
            s.Compras.Conectar(loja);
            Recibo r = null;
            loja.Iniciar(Compras.Catalogo, x => r = x, (p, m) => { });
            s.Compras.Iniciar(Pacote);
            loja.Comprar(Pacote);
            Assert.AreEqual(EstadoCompra.Pendente, s.Compras.Receber(r));
            string gravado = s.Save(T);   // o Concluir gravou; o app morre antes do confirmar
            StringAssert.Contains("\niap.tx=" + r.Tx + "," + Pacote + ",e,", gravado, "gravada como entregue (falta confirmar)");
            Sim l = Sim.Load(gravado);
            l.Compras.Conectar(loja);
            Assert.AreEqual(EstadoCompra.Entregue, l.Compras.Estado(r.Tx), "o save sabe que falta confirmar");
            Assert.AreEqual(1, l.Compras.Concluir(() => throw new InvalidOperationException("nada a gravar"), tx => { }).Count,
                "confirma mesmo se a loja nao reenviar");
            var j = new Jogo(loja, gravado);   // reabre: o save ja tem a entrega; a loja reentrega o nao confirmado
            Assert.IsTrue(j.S.Compras.Possui(Pacote));
            CollectionAssert.AreEqual(new EstadoCompra?[] { EstadoCompra.Entregue }, j.Estados, "repetida: nada a entregar de novo");
            Assert.AreEqual(0, j.Gravacoes, "sem pendente: nada a gravar antes de confirmar");
            Assert.AreEqual(1, j.S.Compras.Transacoes);
            Assert.AreEqual(0, loja.NaoConfirmadas, "so confirmou");
            Assert.AreEqual(EstadoCompra.Confirmada, j.S.Compras.Estado(r.Tx));
        }

        [Test]
        public void Exploit_CompraDuplicada_MesmoRecibo2x_Entrega1x()
        {
            var j = new Jogo(new LojaFalsa(null, LojaFalsa.Cenario.Duplica));
            j.Comprar(Pacote);
            CollectionAssert.AreEqual(new EstadoCompra?[] { EstadoCompra.Pendente, EstadoCompra.Entregue }, j.Estados, "o 2o e' repetido");
            Assert.AreEqual(1, j.Gravacoes);
            Assert.AreEqual(1, j.S.Compras.Transacoes);
            var s = new Sim();
            s.Compras.Conectar(j.L);
            Recibo r = LojaFalsa.Assina("fake.dup", Pacote);
            for (int i = 0; i < 3; i++) Assert.AreEqual(EstadoCompra.Pendente, s.Compras.Receber(r), "antes do save, segue pendente");
            Assert.AreEqual(1, s.Compras.Transacoes, "uma transacao, uma entrega");
        }

        [Test]
        public void Exploit_Restore2x_EDepoisDeReinstalar_DevolveOPacoteUmaVez()
        {
            string conta = Temp();
            try
            {
                var j = new Jogo(new LojaFalsa(conta));
                j.Comprar(Pacote);
                int n = -1;
                j.L.Restaurar(x => n = x);
                j.L.Restaurar(x => n = x);
                Assert.AreEqual(1, n);
                Assert.AreEqual(1, j.S.Compras.Transacoes, "restore 2x: a mesma transacao");
                Assert.AreEqual(1, j.Gravacoes, "nada novo a gravar");
                var k = new Jogo(new LojaFalsa(conta));   // reinstalou: sem save, a mesma conta da loja
                Assert.IsFalse(k.S.Compras.Possui(Pacote), "instalacao limpa: nada ate restaurar");
                k.L.Restaurar(x => n = x);
                Assert.AreEqual(1, n);
                Assert.IsTrue(k.S.Compras.Possui(Pacote), "restore devolve o Pacote do Mestre");
                Assert.IsTrue(k.S.Anuncios.RemoveAds);
                Assert.AreEqual(1, k.Gravacoes, "e grava");
                k.L.Restaurar(x => n = x);
                Assert.AreEqual(1, k.S.Compras.Transacoes);
                k.Comprar(Pacote);
                Assert.AreEqual(1, k.S.Compras.Transacoes, "comprar de novo o que ja e' dele: nem chega a loja");
            }
            finally { File.Delete(conta); }
        }

        [Test]
        public void Exploit_ReciboDeOutroProdutoOutroAppForjadoOuSemLoja_NaoEntrega()
        {
            var loja = new LojaFalsa(null);
            var s = new Sim();
            s.Compras.Conectar(loja);
            Assert.IsTrue(s.Compras.Iniciar(Pacote));
            Assert.IsNull(s.Compras.Receber(LojaFalsa.Assina("fake.1", "iap_gems_100")), "produto fora do catalogo (assinado)");
            Assert.AreEqual(Pacote, s.Compras.Iniciada, "recibo de outro produto nao fecha a compra em curso");
            Assert.IsNull(s.Compras.Receber(LojaFalsa.Assina("fake.2", Pacote, "com.outro.jogo")), "recibo de outro app");
            Assert.IsNull(s.Compras.Iniciada, "a loja respondeu esta compra (recusada)");
            Recibo ok = LojaFalsa.Assina("fake.3", Pacote);
            Assert.IsNull(s.Compras.Receber(new Recibo { Tx = "fake.4", Produto = Pacote, Payload = ok.Payload, Assinatura = ok.Assinatura }), "recibo de outra transacao");
            Assert.IsNull(s.Compras.Receber(new Recibo { Tx = ok.Tx, Produto = Pacote, Payload = ok.Payload, Assinatura = "00" + ok.Assinatura.Substring(2) }), "assinatura mexida");
            Assert.IsNull(s.Compras.Receber(LojaFalsa.Assina("fake,5\niap.tx=x", Pacote)), "ID de transacao fora do formato (quebraria o save)");
            Assert.IsNull(s.Compras.Receber(null));
            Assert.AreEqual((0, false), (s.Compras.Transacoes, s.Compras.Possui(Pacote)));
            var semLoja = new Sim();
            semLoja.Compras.Conectar(null);
            Assert.IsFalse(semLoja.Compras.Iniciar(Pacote), "build sem loja: nada a comprar");
            Assert.IsNull(semLoja.Compras.Receber(ok), "sem loja nenhum recibo vale");
        }

        [Test]
        public void Exploit_RelogioParaTras_NaoTiraNemDuplicaACompra()
        {
            string conta = Temp();
            try
            {
                var j = new Jogo(new LojaFalsa(conta));
                j.Comprar(Pacote);
                string atras = Sim.Load(j.Disco).Save(T - 30 * Calendario.Dia);   // fechou com o relogio 30 dias atras
                var k = new Jogo(new LojaFalsa(conta), atras);
                Assert.IsTrue(k.S.Compras.Possui(Pacote), "a posse nao depende do relogio");
                k.L.Restaurar(x => { });
                Assert.AreEqual(1, k.S.Compras.Transacoes, "nem duplica");
                Assert.AreEqual(0, k.Gravacoes, "nada novo a entregar");
            }
            finally { File.Delete(conta); }
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta_SemCompraOTextoNaoMuda_SaveAntigoNadaComprado()
        {
            string limpo = new Sim().Save(T);
            StringAssert.DoesNotContain("iap.", limpo, "sem compra: o save fica como antes");
            Assert.AreEqual(0, Sim.Load(CoreTests.SaveV03).Compras.Transacoes, "save antigo: nada comprado");
            var j = new Jogo(new LojaFalsa(null));
            j.Comprar(Pacote);
            string text = j.S.Save(T);
            string tx = FirstTx(text);
            StringAssert.StartsWith("fake.", tx);
            StringAssert.Contains("\niap.tx=" + tx + "," + Pacote + ",c,", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual(text, b.Save(T), "save estavel");
            Assert.IsFalse(b.Compras.Possui(Pacote), "Sim.Load sozinho (bot, teste) nao valida recibo: nenhuma posse");
            b.Compras.Conectar(j.L);
            Assert.IsTrue(b.Compras.Possui(Pacote));
            Assert.AreEqual(EstadoCompra.Confirmada, b.Compras.Estado(tx));
        }

        static string FirstTx(string save)
        {
            string l = save.Split('\n').First(x => x.StartsWith("iap.tx=", StringComparison.Ordinal));
            return l.Substring(7).Split(',')[0];
        }

        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

        [Test]
        public void SaveEditado_EntitlementSemReciboValido_NaoVale()
        {
            var loja = new LojaFalsa(null);
            string limpo = new Sim().Save(T);
            Sim Abre(string extra)
            {
                Sim s = Sim.Load(limpo + extra);
                s.Compras.Conectar(loja);
                return s;
            }
            Sim a = Abre("iap.posse=remove_ads\nremove_ads=1\nad.remove_ads=1\niap.pacote=1\n");
            Assert.AreEqual((false, false, 0), (a.Compras.Tem(Compras.RemoveAds), a.Anuncios.RemoveAds, a.Compras.Transacoes), "booleano solto nao existe");
            Sim f = Abre("iap.tx=fake.x," + Pacote + ",c," + B64("fs-dev|br.com.vstack.forgestreet|" + Pacote + "|fake.x") + "," + B64("deadbeef") + "\n");
            Assert.AreEqual((false, 1), (f.Compras.Possui(Pacote), f.Compras.Transacoes), "recibo forjado: so a linha do ledger, sem posse");
            Recibo ok = LojaFalsa.Assina("fake.ok", Pacote);
            string linha = "iap.tx=fake.ok," + Pacote + ",c," + B64(ok.Payload) + "," + B64(ok.Assinatura) + "\n";
            Assert.IsTrue(Abre(linha).Compras.Possui(Pacote), "o recibo valido vale (re-derivado)");
            Assert.IsFalse(Abre(linha.Replace("fake.ok,", "fake.ou,")).Compras.Possui(Pacote), "trocar o ID da transacao invalida");
            Sim g = Abre("iap.tx=a,b\niap.tx=," + Pacote + ",c,,\niap.tx=x y," + Pacote + ",c,,\niap.tx=k," + Pacote + ",z,,\niap.tx=m," + Pacote + ",c,%%,!!\n");
            Assert.AreEqual(1, g.Compras.Transacoes, "lixo ignorado (so a ultima linha tem formato; base64 ruim = recibo vazio)");
            Assert.IsFalse(g.Compras.Possui(Pacote));
            Sim semLoja = Sim.Load(limpo + linha);
            semLoja.Compras.Conectar(null);
            Assert.IsFalse(semLoja.Compras.Possui(Pacote), "build sem loja: recibo nenhum vale");
        }

        // ------------------------------------------------------------------ Pacote do Mestre (E-PLAT-05)

        static Sim Pronto()
        {
            Sim s = TuningTests.Liberado();
            s.FirstSaleTime = 1f; s.VipIn = 200f;
            s.OrderItem = (int)Item.Sword; s.OrderTarget = 5; s.OrderReward = 100;
            s.LastClaim = 777; s.LastClaimGold = 400;
            return s;
        }

        static void Entrega(Sim s, LojaFalsa loja)
        {
            s.Compras.Conectar(loja);
            Assert.AreEqual(EstadoCompra.Pendente, s.Compras.Receber(LojaFalsa.Assina("fake.p", Pacote)));
        }

        [Test]
        public void SemVideo_SoComOPacote_SoNosPlacementsDaV20_MesmasRecargasETetos()
        {
            Sim s = Pronto();
            foreach (Anuncio a in Enum.GetValues(typeof(Anuncio))) Assert.IsFalse(s.Compras.SemVideoEm(a), a + ": sem o pacote, com video");
            Entrega(s, new LojaFalsa(null));
            foreach (Anuncio a in Enum.GetValues(typeof(Anuncio)))
                Assert.AreEqual(a == Anuncio.Velocidade || a == Anuncio.EncomendaTroca || a == Anuncio.Supply, s.Compras.SemVideoEm(a), a.ToString());
            string tx = s.Anuncios.Oferta(Anuncio.Supply);
            Assert.AreEqual(15L, s.Anuncios.Pagar(tx), "mesma recompensa mostrada antes");
            Assert.AreEqual(-1L, s.Anuncios.Pagar(tx), "toque duplo: o mesmo ID nao paga 2x");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Supply), "mesma recarga de 10 min");
            Assert.AreEqual(1, s.Anuncios.HojeTotal(), "conta no teto diario");
            Sim cedo = new Sim();
            Entrega(cedo, new LojaFalsa(null));
            Assert.IsFalse(cedo.Anuncios.Pode(Anuncio.Supply), "D6: nada antes da 1a melhoria, com ou sem video");
        }

        [Test]
        public void RemoveAds_DoRecibo_VetaOIntersticial_ORewardedContinua()
        {
            var s = new Sim(new Tuning { Interstitial = true });
            s.Buy(Upgrade.Tools);
            bool Transicao() => Estados.De(true, true, false, false, false, true) == Estado.Transition;
            Assert.IsNull(s.Anuncios.VetoIntersticial(Transicao(), false, 1e6f));
            Entrega(s, new LojaFalsa(null));
            Assert.AreEqual("remove_ads", s.Anuncios.VetoIntersticial(Transicao(), false, 1e6f));
            Sim b = Sim.Load(s.Save(T), s.T);
            Assert.IsNull(b.Anuncios.VetoIntersticial(Transicao(), false, 1e6f), "sem a loja para validar, o save sozinho nao tira o intersticial");
            b.Compras.Conectar(new LojaFalsa(null));
            Assert.AreEqual("remove_ads", b.Anuncios.VetoIntersticial(Transicao(), false, 1e6f), "com a loja: re-derivado do recibo");
        }

        [Test]
        public void LojaFalsa_Recusa_DevolveOBotao_EOutroAppNaoValida()
        {
            var j = new Jogo(new LojaFalsa(null, LojaFalsa.Cenario.Recusa));
            j.Comprar(Pacote);
            CollectionAssert.AreEqual(new[] { Pacote + ":recusada" }, j.Falhas);
            Assert.IsNull(j.S.Compras.Iniciada);
            Assert.IsTrue(j.S.Compras.PodeComprar(Pacote), "pode tentar de novo");
            Assert.AreEqual((LojaFalsa.Cenario.Interrompe, LojaFalsa.Cenario.Duplica, LojaFalsa.Cenario.Recusa, LojaFalsa.Cenario.Normal),
                (LojaFalsa.Ler("interrompe"), LojaFalsa.Ler("duplica"), LojaFalsa.Ler("recusa"), LojaFalsa.Ler("")));
            Assert.AreEqual("R$ 14,90", j.L.Preco(Pacote), "preco que a loja devolve (placeholder)");
            Assert.IsNull(j.L.Preco("iap_nada"));
        }

        // ------------------------------------------------------------------ anuncio (E-PLAT-07)

        [Test]
        public void Exploit_AnuncioDuplicado_ViradaDoDiaEReabrirNaoReabremOIdPago()
        {
            Sim s = Pronto();
            long meioDia = 20 * Calendario.Dia + 12 * 3600;
            s.Metas.Agora(meioDia, 0);
            string tx = s.Anuncios.Oferta(Anuncio.EncomendaBonus);
            Assert.Greater(s.Anuncios.Pagar(tx), 0L);
            s.Metas.Agora(meioDia + Calendario.Dia, 0);
            Assert.AreEqual(0, s.Anuncios.HojeTotal(), "dia novo zera o teto");
            Assert.AreEqual(-1L, s.Anuncios.Pagar(tx), "mas o ID pago nao volta a pagar");
            Sim b = Sim.Load(s.Save(T));
            b.Metas.Agora(meioDia + 2 * Calendario.Dia, 0);
            Assert.AreEqual(-1L, b.Anuncios.Pagar(tx), "nem depois de reabrir em outro dia");
        }
    }
}
