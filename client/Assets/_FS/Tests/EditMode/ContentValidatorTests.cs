using System;
using System.Collections.Generic;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// A-CORE-05: o conteudo atual passa no ContentValidator, e cada regra fica vermelha com um defeito injetado numa COPIA (tabela
    /// clonada ou mundo novo): as tabelas reais nunca mudam aqui. Cada negativo passa pelo Validate inteiro, entao prova a regra e a
    /// ligacao dela.
    /// </summary>
    public class ContentValidatorTests
    {
        static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        static UpgradeDef[] Ups() => (UpgradeDef[])Upgrades.All.Clone();

        /// <summary>A definicao real de `id`, trocando so o que vier.</summary>
        static UpgradeDef With(Upgrade id, int? requires = null, string name = null, string desc = null, bool? luxury = null, bool? menu = null, string key = null)
        {
            UpgradeDef d = Upgrades.All[(int)id];
            return new UpgradeDef(id, key ?? d.Id, requires ?? d.Requires, name ?? d.Name, desc ?? d.Desc, luxury ?? d.Luxury, menu ?? d.InMenu);
        }

        /// <summary>A-CORE-04: os IDs que o save grava, nesta ordem (os 29 primeiros = Sim.IdsV1, a ordem do enum na 0.1-0.7). Renomear ou apagar
        /// um quebra o save de quem ja jogou: so com passo em Sim.Migrations, e entao este golden muda junto. ID novo: anexe aqui no mesmo PR.</summary>
        static readonly string[] Golden =
        {
            "fole", "bigorna2", "ajudante1", "escudos", "mochila", "ajudante2", "esteira", "fole_duplo", "botas", "ferramentas", "vitrine", "fornalha2",
            "ajudante3", "ajudantes_ageis", "martelo_veloz", "corredor", "joalheria", "joalheiro1", "lupa", "vitrine_joias", "fachada_nobre",
            "piso_oficina", "joalheria_real", "mineiro", "joalheiro2", "balcao5", "balcao6", "balcao7", "balcao8",
            // B-CORE-03: niveis de estacao (Meta)
            "bigornas_nivel2", "bigornas_nivel3", "escudos_nivel2", "escudos_nivel3", "ferramentas_nivel2", "ferramentas_nivel3", "joalheria_nivel2", "joalheria_nivel3",
            // B-CORE-07: niveis IV-V (com projeto) e a linha da espada longa
            "bigornas_nivel4", "bigornas_nivel5", "escudos_nivel4", "escudos_nivel5", "ferramentas_nivel4", "ferramentas_nivel5", "joalheria_nivel4", "joalheria_nivel5",
            "bigorna_longa",
        };

        [Test]
        public void ConteudoAtual_SemViolacao()
        {
            List<string> v = ContentValidator.Validate();
            Assert.IsEmpty(v, string.Join("\n", v));
        }

        [Test]
        public void Ids_Golden_NenhumIdGravadoSumiuOuMudou()
        {
            var atual = new List<string>(Array.ConvertAll(Upgrades.All, d => d.Id));
            foreach (string id in Golden) Assert.IsTrue(atual.Contains(id), $"\"{id}\" sumiu da tabela: renomear/apagar ID pede passo em Sim.Migrations");
            foreach (string id in atual) Assert.IsTrue(Array.IndexOf(Golden, id) >= 0, $"\"{id}\" e' novo: anexe no fim do Golden (e nunca mais o renomeie)");
            Assert.AreEqual(29, Sim.IdsV1.Length, "a lista do schema 1 e' congelada");
            for (int i = 0; i < Sim.IdsV1.Length; i++) Assert.AreEqual(Golden[i], Sim.IdsV1[i], "Sim.IdsV1 congelada: e' como o up= da 0.1-0.7 se le");
        }

        [Test]
        public void Ids_SemIdForaDoFormatoRepetidoESumido_Acusa()
        {
            var a = Ups();
            a[(int)Upgrade.Helper1] = With(Upgrade.Helper1, key: "");
            a[(int)Upgrade.Shields] = With(Upgrade.Shields, key: "Escudos");
            a[(int)Upgrade.Tools] = With(Upgrade.Tools, key: "ferramenta-1");
            a[(int)Upgrade.Conveyor] = With(Upgrade.Conveyor, key: "bigorna2");
            a[(int)Upgrade.FurnaceSpeed1] = With(Upgrade.FurnaceSpeed1, key: "fole_simples");   // "renomeado" sem migracao
            var v = ContentValidator.Validate(upgrades: a);
            Acusa(v, "id: Helper1 sem ID estavel");
            Acusa(v, "id: \"Escudos\" (Shields) fora de [a-z0-9_]");
            Acusa(v, "id: \"ferramenta-1\" (Tools) fora de [a-z0-9_]");
            Acusa(v, "id: \"bigorna2\" repetido");
            Acusa(v, "id: \"fole\" do schema 1 sumiu da tabela");
            Acusa(v, "id: \"esteira\" do schema 1 sumiu da tabela");
        }

        [Test]
        public void Ids_ForaDeOrdemERepetido_Acusa()
        {
            var a = Ups();
            a[(int)Upgrade.Anvil2] = Upgrades.All[(int)Upgrade.FurnaceSpeed1];
            var v = ContentValidator.Validate(upgrades: a);
            Acusa(v, "id: tabela[1] e' FurnaceSpeed1, fora da ordem");
            Acusa(v, "id: FurnaceSpeed1 repetido");
        }

        [Test]
        public void Requisitos_InexistenteOuDeTierMaior_Acusa()
        {
            var a = Ups();
            a[(int)Upgrade.Anvil2] = With(Upgrade.Anvil2, requires: 99);
            a[(int)Upgrade.Helper1] = With(Upgrade.Helper1, requires: (int)Upgrade.Tools);
            var v = ContentValidator.Validate(upgrades: a);
            Acusa(v, "requisito: Anvil2 exige 99, que nao existe");
            Acusa(v, "requisito: Helper1 exige Tools, de tier igual ou maior");
        }

        [Test]
        public void Alcance_CicloProdutivoQueExigeLuxoESemPad_Acusa()
        {
            // ciclo: Ajudante -> Ajudante 3 -> Ajudante 2 -> Ajudante; os Ajudantes ageis (exigem o Ajudante) caem junto
            var ciclo = Ups();
            ciclo[(int)Upgrade.Helper1] = With(Upgrade.Helper1, requires: (int)Upgrade.Helper3);
            var v = ContentValidator.Validate(upgrades: ciclo);
            foreach (Upgrade u in new[] { Upgrade.Helper1, Upgrade.Helper2, Upgrade.Helper3, Upgrade.HelperSpeed }) Acusa(v, $"alcance: {u} nunca fica a venda");
            // produtivo que exige luxo: o luxo espera toda a producao, que espera o luxo
            var luxo = Ups();
            luxo[(int)Upgrade.Miner] = With(Upgrade.Miner, requires: (int)Upgrade.WorkshopFacade);
            v = ContentValidator.Validate(upgrades: luxo);
            Acusa(v, "alcance: Miner nunca fica a venda");
            Acusa(v, "alcance: WorkshopFacade nunca fica a venda");
            // upgrade de pad sem pad
            Sim w = ContentValidator.World();
            w.Pads.RemoveAll(p => Array.IndexOf(p.Chain, Upgrade.Conveyor) >= 0);
            Acusa(ContentValidator.Validate(world: w), "alcance: Conveyor sem pad e fora do menu");
        }

        [Test]
        public void Luxo_MarcaErrada_Acusa()
        {
            var a = Ups();
            a[(int)Upgrade.Miner] = With(Upgrade.Miner, luxury: true);                    // produtivo marcado como luxo: a conta nao fecha
            a[(int)Upgrade.WorkshopFloor] = With(Upgrade.WorkshopFloor, menu: true);       // luxo no menu
            a[(int)Upgrade.WorkshopFacade] = With(Upgrade.WorkshopFacade, requires: -1);   // luxo sem requisito
            Sim w = ContentValidator.World();
            w.FurnaceB.UnlockBy = (int)Upgrade.JewelryDecor;                               // estacao aberta por luxo
            var v = ContentValidator.Validate(upgrades: a, world: w);
            Acusa(v, $"luxo: {Upgrades.ProductionCount - 1} produtivos marcados, Upgrades.ProductionCount = {Upgrades.ProductionCount}");
            Acusa(v, "luxo: WorkshopFloor no menu");
            Acusa(v, "luxo: WorkshopFacade sem requisito");
            Acusa(v, "luxo: JewelryDecor abre a estacao Fornalha 2");
        }

        [Test]
        public void Custos_ZeroForaDoMultiploECurvaQueDesce_Acusa()
        {
            long[] c = ContentValidator.RealCosts();
            c[(int)Upgrade.Helper2] = 0;
            c[(int)Upgrade.PlayerSpeed] = c[(int)Upgrade.FurnaceSpeed2] - 5;   // a formula sobe tier a tier; aqui desce
            c[(int)Upgrade.Miner] = 3001;
            var v = ContentValidator.Validate(costs: c);
            Acusa(v, "custo: Helper2 = 0");
            Acusa(v, $"custo: PlayerSpeed = {c[(int)Upgrade.PlayerSpeed]} nao sobe do tier anterior");
            Acusa(v, "custo: Miner = 3001, fora do multiplo de 5");
        }

        [Test]
        public void Nomes_VazioLongoESemDescricao_Acusa()
        {
            var a = Ups();
            a[(int)Upgrade.FurnaceSpeed1] = With(Upgrade.FurnaceSpeed1, name: " ");
            a[(int)Upgrade.Anvil2] = With(Upgrade.Anvil2, name: "Bigorna de dois martelos", desc: "");
            var v = ContentValidator.Validate(upgrades: a);
            Acusa(v, "texto: FurnaceSpeed1 sem nome");
            Acusa(v, $"texto: Anvil2 \"Bigorna de dois martelos\" passa de {ContentValidator.NameMax}");
            Acusa(v, "texto: Anvil2 sem descricao");
        }

        /// <summary>Receitas reais trocando so a da familia `station`.</summary>
        static RecipeDef[] Rec(string station, RecipeDef r) => Array.ConvertAll(Recipes.All, x => x.Station == station ? r : x);

        /// <summary>B-CORE-01: itens reais + cabo (insumo) e machado (produto), anexados depois dos reais, so no teste.</summary>
        static readonly Item Cabo = (Item)Items.All.Length, Machado = (Item)(Items.All.Length + 1);
        static ItemDef[] ComMachado() => new List<ItemDef>(Items.All) { new ItemDef("cabo", "cabo", 2, false), new ItemDef("machado", "machado", 2, true) }.ToArray();

        [Test]
        public void Receitas_SemPrecoSemEstacaoSemCompradorESemInsumo_Acusa()
        {
            int[] preco = (int[])Tuning.Padrao.Price.Clone();
            preco[(int)Item.Shield] = 0;
            Sim w = ContentValidator.World(null, Rec("bigorna", new RecipeDef("bigorna", Kind.Crafter, Item.Sword, t => 1f, (Item.Ingot, 0))));
            w.Stations.Remove(w.ToolBench);                      // ferramenta sem bancada
            w.Stations.Remove(w.JewelShop);                      // joia sem loja
            w.Stations.RemoveAll(s => s.Kind == Kind.Furnace);   // sem fornalha: minerio sem comprador, bigornas sem lingote
            var v = ContentValidator.Validate(prices: preco, world: w);
            Acusa(v, "receita: escudo sem preco");
            Acusa(v, "receita: ferramenta sem estacao que produz");
            Acusa(v, "receita: ferramenta sem estacao da familia ferramentas");
            Acusa(v, "receita: joia produzido sem comprador");
            Acusa(v, "receita: minerio produzido sem comprador");
            Acusa(v, "receita: espada usa lingote, que nenhuma estacao produz");
            Acusa(v, "receita: espada com insumo opcional (0) fora da regra");   // C-CORE-05: 0 sozinho nao e' o slot de liga
        }

        [Test]
        public void Receitas_EstacaoSemReceitaOuDeOutroTipo_Acusa()
        {
            RecipeDef[] r = Array.FindAll(Recipes.All, x => x.Station != "joalheria");   // a Joalheria fica sem receita
            r = Array.ConvertAll(r, x => x.Station == "escudos" ? new RecipeDef("escudos", Kind.Furnace, Item.Shield, t => 1f, (Item.Ingot, 2)) : x);
            var v = ContentValidator.Validate(recipes: r);
            Acusa(v, "receita: Joalheria sem receita");
            Acusa(v, "receita: Escudos (Crafter) com receita de Furnace");
        }

        /// <summary>B-CORE-01, aceite: machado = 1 cabo + 2 lingotes (2 insumos diferentes) na familia ferramentas. Sem quem faca o cabo o
        /// validator acusa o insumo; com a familia escudos fazendo cabo do lingote, essa regra cala; cabo feito de machado e' ciclo.</summary>
        [Test]
        public void Receitas_DoisInsumos_InsumoQueNinguemProduzECiclo_Acusa()
        {
            var machado = new RecipeDef("ferramentas", Kind.Crafter, Machado, t => 1f, (Cabo, 1), (Item.Ingot, 2));
            int[] preco = new List<int>(Tuning.Padrao.Price) { 0, 30 }.ToArray();
            RecipeDef[] semCabo = Rec("ferramentas", machado);
            var v = ContentValidator.Validate(prices: preco, items: ComMachado(), recipes: semCabo);
            Acusa(v, "receita: machado usa cabo, que nenhuma estacao produz");
            Assert.IsFalse(v.Exists(m => m.Contains("usa lingote")), "o outro insumo tem quem faca");

            RecipeDef[] comCabo = Array.ConvertAll(semCabo, x => x.Station == "escudos" ? new RecipeDef("escudos", Kind.Crafter, Cabo, t => 1f, (Item.Ingot, 1)) : x);
            v = ContentValidator.Validate(prices: preco, items: ComMachado(), recipes: comCabo);
            Assert.IsFalse(v.Exists(m => m.Contains("usa cabo") || m.Contains("machado")), string.Join("\n", v));
            Acusa(v, "receita: escudo sem estacao que produz");   // a familia escudos agora faz cabo: so o escudo fica orfao

            RecipeDef[] ciclo = Array.ConvertAll(semCabo, x => x.Station == "escudos" ? new RecipeDef("escudos", Kind.Crafter, Cabo, t => 1f, (Machado, 1)) : x);
            v = ContentValidator.Validate(prices: preco, items: ComMachado(), recipes: ciclo);
            Acusa(v, "receita: machado usa cabo, que nunca sai do minerio (ciclo)");
            Acusa(v, "receita: cabo usa machado, que nunca sai do minerio (ciclo)");
        }

        [Test]
        public void Itens_IdForaDoFormatoRepetidoForaDaOrdemTierESemNome_Acusa()
        {
            var a = (ItemDef[])Items.All.Clone();
            a[(int)Item.Shield] = new ItemDef("Escudo", "escudo", 1, true);
            a[(int)Item.Tool] = new ItemDef("espada", "ferramenta", 6, true);
            a[(int)Item.Jewel] = new ItemDef("joia", " ", 3, true);
            var v = ContentValidator.Validate(items: a);
            Acusa(v, "item 3: ID \"Escudo\" vazio ou fora de [a-z0-9_]");
            Acusa(v, "item: \"espada\" repetido");
            Acusa(v, "item 4: \"espada\" no lugar de \"ferramenta\"");
            Acusa(v, "item: espada com tier 6");
            Acusa(v, "item: joia sem nome");
        }

        /// <summary>B-CORE-01: os IDs de item que o save grava (schema 3) e a ordem congelada do schema 2 (espelho e migracao). Mesmo contrato do
        /// Golden dos upgrades: renomear ou apagar pede passo em Sim.Migrations; item novo entra no fim.</summary>
        [Test]
        public void Itens_Golden_IdsGravadosEOrdemDoSchema2()
        {
            string[] v2 = { "minerio", "lingote", "espada", "escudo", "ferramenta", "joia" };
            string[] golden = { "minerio", "lingote", "espada", "escudo", "ferramenta", "joia", "espada_longa" };   // B-CORE-07: espada longa no fim
            CollectionAssert.AreEqual(golden, Array.ConvertAll(Items.All, d => d.Id));
            CollectionAssert.AreEqual(v2, Sim.ItemIdsV2, "Sim.ItemIdsV2 congelada: e' como o stock=/hold= por posicao se le");
            Assert.AreEqual(Enum.GetValues(typeof(Item)).Length, golden.Length, "o enum Item e' atalho das primeiras posicoes");
        }

        [Test]
        public void Marcos_SemOuroSemRotuloEInalcancavel_Acusa()
        {
            V2 p = Balance.WorkshopChest;
            var m = new[]
            {
                new MilestoneDef((int)Item.Sword, 15, 0, p, "15 espadas!"),
                new MilestoneDef((int)Item.Ingot, 10, 60, p, ""),   // lingote nunca vai ao balcao
                new MilestoneDef(-1, 0, 60, p, "0 vendas"),
                new MilestoneDef(9, 5, 60, p, "item 9"),
            };
            var v = ContentValidator.Validate(milestones: m);
            Acusa(v, "marco 0: ouro 0");
            Acusa(v, "marco 1: sem rotulo");
            for (int k = 1; k <= 3; k++) Acusa(v, $"marco {k}: condicao inalcancavel");
        }

        [Test]
        public void Bocas_DentroDeCorpoForaDasParedesSobrePadEZonaSobreposta_Acusa()
        {
            Sim w = ContentValidator.World();
            w.FurnaceA.InAt = w.AnvilA.Pos;                                  // dentro da Bigorna
            w.AnvilA.OutAt = new V2(-1f, 9.5f);                              // fora das paredes
            w.Pads.Find(p => p.Chain[0] == Upgrade.Conveyor).Pos = w.ToolBench.InAt + new V2(0f, 0.5f);
            w.AnvilB.InAt = w.Deposit.InAt + new V2(0.3f, 0f);
            var v = ContentValidator.Validate(world: w);
            Acusa(v, "boca: Fornalha (1.50,9.50) dentro de um corpo");
            Acusa(v, "boca: Bigorna (-1.00,9.50) fora das paredes");
            Acusa(v, "boca: Ferramentas (6.60,5.50) na zona do pad Conveyor");
            Acusa(v, "boca: Bigorna 2 (1.80,2.15) com a zona sobreposta a de Depósito");
        }

        /// <summary>B-CORE-03: preco por nivel crescente com Normal = 100% e Masterwork = 0 (nao vende); nivel de estacao com fonte conhecida
        /// (familia com bancada de produto), no menu, sem ser luxo, e na cadeia da propria familia.</summary>
        [Test]
        public void Qualidade_PrecoForaDeOrdemMasterworkVendidaEFonteDesconhecida_Acusa()
        {
            var w = new Sim(new Tuning { QualidadePct = new[] { 90, 250, 150, 300 }, NivelEstacaoQ = 0 });
            var v = new List<string>(ContentValidator.Qualidade(Upgrades.All, w));
            Acusa(v, "qualidade: Normal a 90%");
            Acusa(v, "qualidade: Superior a 150% nao sobe de Fine (250%)");
            Acusa(v, "qualidade: Masterwork a 300% (nao vende: 0)");
            Acusa(v, "qualidade: 0 pontos por nivel de estacao");
            Acusa(new List<string>(ContentValidator.Qualidade(Upgrades.All, new Sim(new Tuning { QualidadePct = new[] { 100, 150 } }))), "qualidade: 2 precos para 4 niveis");

            var a = Ups();
            UpgradeDef d = Upgrades.All[(int)Upgrade.AnvilsLv2], d3 = Upgrades.All[(int)Upgrade.ShieldsLv3], t2 = Upgrades.All[(int)Upgrade.ToolsLv2];
            a[(int)Upgrade.AnvilsLv2] = new UpgradeDef(d.Up, d.Id, d.Requires, d.Name, d.Desc, menu: true, station: "forja_runica");   // familia que nao existe
            a[(int)Upgrade.ShieldsLv3] = new UpgradeDef(d3.Up, d3.Id, (int)Upgrade.ToolsLv2, d3.Name, d3.Desc, menu: true, station: "escudos");   // exige o nivel de outra familia
            a[(int)Upgrade.ToolsLv2] = new UpgradeDef(t2.Up, t2.Id, t2.Requires, t2.Name, t2.Desc, luxury: true, station: "ferramentas");   // Meta fora do menu e luxo
            v = ContentValidator.Validate(upgrades: a);
            Acusa(v, "qualidade: AnvilsLv2 e' nivel de \"forja_runica\", familia sem bancada de produto (fonte desconhecida)");
            Acusa(v, "qualidade: ShieldsLv3 fora da cadeia de niveis de \"escudos\"");
            Acusa(v, "qualidade: ToolsLv2 (Meta) fora do menu ou marcado como luxo");
        }

        [Test]
        public void Pisaveis_BauDentroDeCorpoEPadForaDasParedes_Acusa()
        {
            Sim w = ContentValidator.World();
            w.Chests[0].Pos = w.FurnaceA.Pos;
            w.Pads.Find(p => p.Chain[0] == Upgrade.Miner).Pos = new V2(14.9f, 0.1f);
            var v = ContentValidator.Validate(world: w);
            Acusa(v, "pisavel: bau 0 (1.50,5.50) dentro de um corpo");
            Acusa(v, "pisavel: pad Miner (14.90,0.10) fora das paredes");
        }
    }
}
