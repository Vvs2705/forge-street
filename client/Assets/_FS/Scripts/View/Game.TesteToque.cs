using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// fix/ui-fechar-ao-tocar-fora: -testetoque pasta (so com -testsession). Teste de ponta a ponta no exe/APK de verdade: um mouse virtual do
    /// Input System toca e arrasta na tela, passando pelo EventSystem, pelo veu (VeuFecha) e pela bandeja (BandejaFora) como um dedo.
    /// Confere as regras (tap fora fecha, arrastar nao, tocar dentro nao, o tap que fecha nao age no mundo, cofre/escolha/bloqueio do
    /// tutorial nao fecham), tira uma foto por passo e sai 0 (tudo ok) ou 1. Linha TOQUEFORA no log.
    /// </summary>
    public sealed partial class Game
    {
        int _toques;   // quantas vezes o toque chegou ao mundo (Game.Toque): o tap que fecha nao pode contar

        /// <summary>Centro (px de tela) do botao ativo cuja legenda e' `rotulo` (o de cima, se houver mais de um).</summary>
        static Vector2 CentroDoBotao(string rotulo)
        {
            Vector2 c = new Vector2(-1f, -1f);
            foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled) continue;
                Text t = b.GetComponentInChildren<Text>();
                if (t == null || t.text != rotulo) continue;
                var k = new Vector3[4];
                ((RectTransform)b.transform).GetWorldCorners(k);   // canvas overlay: canto do mundo = px de tela
                c = (k[0] + k[2]) / 2f;
            }
            return c;
        }

        IEnumerator TesteToqueFora(string pasta)
        {
            Directory.CreateDirectory(pasta);
            Mouse m = InputSystem.AddDevice<Mouse>("ToqueTeste");
            var falhas = new List<string>();
            int passo = 0;
            void Checa(bool ok, string regra)
            {
                Debug.Log("TOQUEFORA " + (ok ? "ok    " : "FALHOU") + " " + regra);
                if (!ok) falhas.Add(regra);
            }
            IEnumerator Foto(string nome)
            {
                yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(pasta, $"{++passo:00}_{nome}.png"));
                yield return null; yield return null;
            }
            IEnumerator Gesto(Vector2 de, Vector2 ate, float seg)
            {
                InputSystem.QueueStateEvent(m, new MouseState { position = de }.WithButton(MouseButton.Left, true));
                yield return null; yield return null;
                for (float t = 0f; t < seg; t += Time.unscaledDeltaTime)
                {
                    InputSystem.QueueStateEvent(m, new MouseState { position = Vector2.Lerp(de, ate, t / seg) }.WithButton(MouseButton.Left, true));
                    yield return null;
                }
                InputSystem.QueueStateEvent(m, new MouseState { position = ate });
                for (int i = 0; i < 4; i++) yield return null;
            }

            yield return new WaitForSecondsRealtime(2f);
            float w = Screen.width, h = Screen.height;
            Vector2 mundo = new Vector2(w * 0.5f, h * 0.5f), veu = new Vector2(w * 0.02f, h * 0.5f);

            // 1. bandeja aberta + TAP no mundo: fecha, e o toque nao chega ao mundo
            if (!_menu.IsOpen) _menu.Toggle();
            yield return Foto("bandeja_aberta");
            int t0 = _toques;
            yield return Gesto(mundo, mundo, 0.08f);
            Checa(!_menu.IsOpen, "tap no mundo fecha a bandeja de melhorias");
            Checa(_toques == t0, "o tap que fechou nao agiu no mundo (Game.Toque)");
            yield return Foto("bandeja_fechada_pelo_tap");

            // 2. bandeja aberta + ARRASTAR (joystick): fica aberta
            _menu.Toggle();
            yield return Gesto(mundo, mundo + new Vector2(0f, h * 0.12f), 0.4f);
            Checa(_menu.IsOpen, "arrastar (joystick) nao fecha a bandeja");
            yield return Foto("bandeja_depois_de_arrastar");

            // 3. tocar DENTRO da bandeja (borda dela, fora dos cartoes) nao fecha
            yield return Gesto(new Vector2(w * 0.025f, _menu.TopPx - 30f), new Vector2(w * 0.025f, _menu.TopPx - 30f), 0.08f);
            Checa(_menu.IsOpen, "tocar dentro da bandeja nao fecha");

            // 4. bloqueio generico (tutorial / boas-vindas registrado): o tap no mundo nao fecha
            System.Func<bool> bloqueia = () => true;
            ToqueFora.Bloqueios += bloqueia;
            yield return Gesto(mundo, mundo, 0.08f);
            Checa(_menu.IsOpen, "com bloqueio registrado (tutorial) o tap nao fecha");
            ToqueFora.Bloqueios -= bloqueia;
            _menu.Toggle();

            // 5. Metas: tap no veu fecha; tap no cartao nao
            _metas.Abrir();
            yield return Foto("metas_aberto");
            yield return Gesto(new Vector2(w * 0.5f, h * 0.9f), new Vector2(w * 0.5f, h * 0.9f), 0.08f);
            Checa(MetasAberto, "tap dentro do cartao de Metas nao fecha");
            yield return Gesto(veu, veu, 0.08f);
            Checa(!MetasAberto, "tap no veu fecha Metas");
            yield return Foto("metas_fechado_pelo_tap");

            // 6. Metas: arrastar no veu nao fecha
            _metas.Abrir();
            yield return Gesto(veu, veu + new Vector2(0f, h * 0.1f), 0.3f);
            Checa(MetasAberto, "arrastar no veu nao fecha Metas");
            // 6b. Conselho do #187: o botao Fechar apertado com um escorregao de 15 px (dedo de verdade) ainda clica
            Vector2 fechar = CentroDoBotao(Loc.T("cfg.fechar"));
            yield return Gesto(fechar, fechar + new Vector2(15f, 6f), 0.12f);
            Checa(!MetasAberto, "Fechar com escorregao de 15 px clica (o veu nao rouba o arraste do botao)");
            if (MetasAberto) _metas.Fechar();

            // 7. configuracoes: tap fora fecha
            _cfg.gameObject.SetActive(true);
            yield return Gesto(new Vector2(w * 0.04f, h * 0.15f), new Vector2(w * 0.04f, h * 0.15f), 0.08f);
            Checa(!Config, "tap fora fecha as configuracoes");

            // 8. painel informativo (detalhe da estacao, -adtest): fecha; cofre (ouro pendente) e escolha: nao
            ShowPanel("Teste", "info", Loc.T("ui.ok"), ClosePanel);
            yield return Gesto(veu, veu, 0.08f);
            Checa(!Painel, "tap fora fecha o painel informativo");
            ShowPanel(Loc.T("cofre.titulo"), Loc.T("cofre.corpo"), Loc.T("cofre.pegar"), ClosePanel, 1234);
            yield return Foto("cofre_aberto");
            yield return Gesto(veu, veu, 0.08f);
            Checa(Painel, "cofre com ouro pendente NAO fecha por toque fora");
            yield return Foto("cofre_continua_aberto");
            Vector2 pegar = CentroDoBotao(Loc.T("cofre.pegar"));
            yield return Gesto(pegar, pegar + new Vector2(-15f, 5f), 0.12f);
            Checa(!Painel, "PEGAR do cofre com escorregao de 15 px clica");
            if (Painel) ClosePanel();
            ShowPanel("Teste", "escolha", "Trocar", () => ClosePanel());
            yield return Gesto(veu, veu, 0.08f);
            Checa(Painel, "painel de escolha NAO fecha por toque fora");
            ClosePanel();

            // 9. sem nada aberto o toque volta a ser do mundo
            t0 = _toques;
            yield return Gesto(mundo, mundo, 0.08f);
            Checa(_toques == t0 + 1, "sem bandeja/painel o tap chega ao mundo");

            InputSystem.RemoveDevice(m);
            Debug.Log(falhas.Count == 0 ? "TOQUEFORA OK" : "TOQUEFORA FALHOU: " + string.Join(" | ", falhas));
            Application.Quit(falhas.Count == 0 ? 0 : 1);
        }
    }
}
