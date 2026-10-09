using System;
using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Arte e som 100% gerados por codigo (zero asset), tecnica do Art.cs do Rune Relay / Formas.cs do ARKANA:
    /// formas com 4 amostras de antialias, cache por chave. Cada ITEM tem cor E forma (minerio pedra, lingote barra,
    /// espada triangulo, escudo losango, ferramenta cruz, joia anel com gema): a cor nunca e' o unico sinal.
    /// </summary>
    public static class Art
    {
        public static readonly Color Bg = Hex(0x121622), Ink = Hex(0xEEF1F8),
            Accent = Hex(0xFFE08A), Bad = Hex(0xFF3B4E), Dim = Hex(0x4A5370), Pad = Hex(0x2C3550), PadFill = Hex(0x6B5F2E),
            Player = Hex(0xFFE08A), Worker = Hex(0x9ED0C8), Client = Hex(0xE8C9A8), Bubble = Hex(0xF4F6FA), Good = Hex(0x4CD964);
        /// <summary>Ouro (ART_BIBLE s2: base #E0B23A, sombra #9C7520, luz = Accent): luxos da fase 3.</summary>
        public static readonly Color Gold = Hex(0xE0B23A), GoldDark = Hex(0x9C7520);

        /// <summary>Cor por Item (indice do enum).</summary>
        public static readonly Color[] ItemColor = { Hex(0x9C8468), Hex(0xC8D0DC), Hex(0x7FC4FF), Hex(0xF2545B), Hex(0x4CD964), Hex(0xB07CF2) };
        public static readonly Color DepositC = Hex(0x5E4F43), FurnaceC = Hex(0xFF8A3D), AnvilC = Hex(0x6C7A93), ShieldC = Hex(0xA64E57), ToolC = Hex(0x3E8E55), CounterC = Hex(0xD9A62E);

        public static Color ComAlfa(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        // ---------- sprites (1 unidade de mundo = sprite inteiro) ----------
        const int Side = 64;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Square() => Get("sq", (x, y) => true);
        public static Sprite Rounded() => Get("rounded", RoundedIn);
        static bool RoundedIn(float x, float y)
        {
            const float r = 0.28f;
            float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
            return qx * qx + qy * qy <= r * r;
        }
        public static Sprite Disc() => Get("disc", (x, y) => x * x + y * y <= 0.96f);
        public static Sprite Ring() => Get("ring", (x, y) => { float d = x * x + y * y; return d <= 0.96f && d >= 0.62f; });
        /// <summary>Anel tracejado (12 tracos): marca de obra no chao dos pads de construcao.</summary>
        public static Sprite DashedRing() => Get("dashring", (x, y) =>
        {
            float d = x * x + y * y;
            return d <= 0.96f && d >= 0.7f && Mathf.Repeat(Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 12f, 1f) < 0.62f;
        });
        public static Sprite Triangle() => Get("tri", (x, y) => y > -0.8f && Mathf.Abs(x) < (0.9f - y) * 0.58f);
        public static Sprite Diamond() => Get("dia", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) < 0.98f);
        public static Sprite Plus() => Get("plus", (x, y) => (Mathf.Abs(x) < 0.3f || Mathf.Abs(y) < 0.3f) && Mathf.Abs(x) < 0.9f && Mathf.Abs(y) < 0.9f);
        public static Sprite Star() => Get("star", (x, y) =>
        {
            const float seg = Mathf.PI * 2f / 5f;
            float f = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(x, y), seg) / seg * 2f - 1f);
            return x * x + y * y <= Mathf.Pow(Mathf.Lerp(0.45f, 0.98f, f * f), 2f);
        });
        /// <summary>Pedra de minerio: disco irregular.</summary>
        public static Sprite Rock() => Get("rock", (x, y) =>
        {
            float a = Mathf.Atan2(y, x);
            float r = 0.86f + 0.1f * Mathf.Sin(a * 3f + 0.7f) + 0.06f * Mathf.Cos(a * 5f);
            return x * x + y * y <= r * r;
        });

        /// <summary>Joia: aro grosso (o furo separa do disco e do losango do escudo a 24 px) com gema em losango no topo (ASSETS #38).</summary>
        public static Sprite Jewel() => Get("jewel", (x, y) =>
        {
            float cy = y + 0.15f, d = x * x + cy * cy;
            bool band = d <= 0.68f * 0.68f && d >= 0.38f * 0.38f;
            bool gem = Mathf.Abs(x) / 0.4f + Mathf.Abs(y - 0.6f) / 0.34f < 1f;
            return band || gem;
        });

        public static Sprite ItemSprite(Item i)
        {
            switch (i)
            {
                case Item.Ore: return Rock();
                case Item.Ingot: return Rounded();
                case Item.Sword: return Triangle();
                case Item.Shield: return Diamond();
                case Item.Jewel: return Jewel();
                default: return Plus();
            }
        }

        /// <summary>Escala da mascara do item (so a reserva sem arte: o lingote e' o quadrado arredondado achatado em barra).</summary>
        public static Vector2 ItemScale(Item i, float s) => i == Item.Ingot ? new Vector2(s * 1.25f, s * 0.6f) : new Vector2(s, s);

        // ---------- arte pre-renderizada dos itens (v0.5, docs/ASSETS.md s7): as mascaras acima viram reserva ----------
        static readonly string[] ItemArtName = { "item_minerio", "item_lingote", "item_espada", "item_escudo", "item_ferramenta", "item_joia" };
        const float ArtFill = 1.1f;   // o item ocupa ~116 dos 128 px da celula: escala = tamanho desejado x 1,1

        /// <summary>Quadro `clip` de uma folha de 1 quadro (Resources/Sprites/&lt;name&gt;); null sem a folha.</summary>
        public static Sprite Icon(string name, string clip) => SpriteSheet.TryGet(name, out SpriteSheet sh) ? sh.Frame(clip, 0, 0f) : null;
        /// <summary>Item renderizado: `icone` (3/4: balao, cartao) ou `deitado` (pilha, estoque, bocas); null sem a folha.</summary>
        public static Sprite ItemArt(Item i, bool icon) => Icon(ItemArtName[(int)i], icon ? "icone" : "deitado");
        /// <summary>Cor base do renderer do item: branco na arte (a cor ja vem no PNG), matiz do item na mascara.</summary>
        public static Color ItemTint(Item i) => ItemArt(i, false) != null ? Color.white : ItemColor[(int)i];

        /// <summary>Pinta o item com `size` m de lado: arte (contorno no PNG, sem achatar o lingote) ou, sem ela, a mascara de sempre.</summary>
        public static void PaintItem(SpriteRenderer r, Item i, bool icon, float size)
        {
            Sprite art = ItemArt(i, icon);
            r.sprite = art != null ? art : ItemSprite(i);
            r.color = art != null ? Color.white : ItemColor[(int)i];
            Vector2 s = art != null ? Vector2.one * (size * ArtFill) : ItemScale(i, size);
            r.transform.localScale = new Vector3(s.x, s.y, 1f);
        }

        /// <summary>
        /// Anel de paciencia do balao: faixa na borda do retangulo arredondado (o mesmo do Rounded), cheia no sentido horario a
        /// partir do topo ate `frac`. ponytail: 24 degraus em cache (24 x 16 KB); trocar por shader radial se o degrau aparecer.
        /// </summary>
        public static Sprite BalloonRing(float frac)
        {
            int k = Mathf.Clamp(Mathf.CeilToInt(frac * 24f), 0, 24);
            return Get("arc" + k, (x, y) =>
                RoundedIn(x, y) && !RoundedIn(x / 0.86f, y / 0.84f) && Mathf.Repeat(Mathf.Atan2(x, y) / (Mathf.PI * 2f), 1f) < k / 24f);
        }

        // ---------- v0.5b (BENCHMARK_VISUAL P1-1..P1-6): placa de obra, juice e seta da dica ----------

        /// <summary>Borda tracejada (16 tracos) de um quadrado arredondado (o mesmo do Rounded): placa de obra no chao.</summary>
        public static Sprite DashedBox() => Get("dashbox", (x, y) =>
            RoundedIn(x, y) && !RoundedIn(x / 0.88f, y / 0.88f) && Mathf.Repeat(Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 16f + 0.25f, 1f) < 0.6f);
        /// <summary>Coracao (curva implicita classica) da venda.</summary>
        public static Sprite Heart() => Get("heart", (x, y) =>
        {
            float X = x * 1.25f, Y = y * 1.25f + 0.2f, a = X * X + Y * Y - 1f;
            return a * a * a - X * X * Y * Y * Y <= 0f;
        });
        /// <summary>Brilho de 4 pontas (astroide) do estouro do balao.</summary>
        public static Sprite Sparkle() => Get("sparkle", (x, y) => Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y)) <= 0.98f);
        /// <summary>Tracos do rosto bravo (sobrancelhas em V, olhos, boca para baixo), desenhados por cima de um Disc laranja.</summary>
        public static Sprite AngryFace() => Get("angry", (x, y) =>
        {
            float ax = Mathf.Abs(x);
            bool brow = ax > 0.12f && ax < 0.62f && Mathf.Abs(y - (0.08f + 0.45f * ax)) < 0.09f;   // sobe para fora: V bravo
            bool eye = (ax - 0.33f) * (ax - 0.33f) + (y + 0.05f) * (y + 0.05f) < 0.012f;
            float d = Mathf.Sqrt(x * x + (y + 0.95f) * (y + 0.95f));
            bool mouth = Mathf.Abs(d - 0.52f) < 0.075f && y > -0.6f && ax < 0.38f;                    // arco de cima de um circulo baixo: boca para baixo
            return brow || eye || mouth;
        });
        /// <summary>Seta da dica apontando para baixo (haste + ponta); `fat` = contorno (desenhado atras, escuro).</summary>
        public static Sprite Arrow(bool fat) => Get(fat ? "arrow+" : "arrow", (x, y) =>
        {
            float g = fat ? 0.12f : 0f;
            bool shaft = Mathf.Abs(x) < 0.26f + g && y > -0.1f && y < 0.86f + g;
            bool head = y < 0.06f + g && y > -0.92f - g && Mathf.Abs(x) < (y + 0.92f + g) * 0.85f;
            return shaft || head;
        });
        /// <summary>Capsula para SpriteRenderer Sliced: pontas de 0,5 unidade fixas, meio estica. Usar size = (w/h, 1) e escala h.</summary>
        public static Sprite Capsule()
        {
            if (Cache.TryGetValue("capsule", out Sprite s) && s != null) return s;
            s = Sprite.Create(Disc().texture, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), Side, 0,
                SpriteMeshType.FullRect, new Vector4(Side / 2 - 1, 0, Side / 2 - 1, 0));
            Cache["capsule"] = s;
            return s;
        }

        /// <summary>Retangulo arredondado 9-fatias para uGUI (Image.Type.Sliced): canto de ~20 px na referencia 1080x1920 em qualquer tamanho.</summary>
        public static Sprite Box()
        {
            if (Cache.TryGetValue("box9", out Sprite s) && s != null) return s;
            const float r = 20f / 64f * 2f;   // raio de 20 px na textura de 64 (coordenadas [-1, 1])
            Sprite m = Get("box9mask", (x, y) =>
            {
                float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
                return qx * qx + qy * qy <= r * r;
            });
            s = Sprite.Create(m.texture, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
            Cache["box9"] = s;
            return s;
        }

        /// <summary>Vinheta: escurece so as bordas (alfa 0 no centro, ~1 nos cantos); Image por cima do mundo e abaixo da HUD.</summary>
        public static Sprite Vignette() => Soft("vignette", (x, y) => Mathf.Clamp01((x * x + y * y - 0.55f) / 1.2f));

        /// <summary>Contorno escuro + sombra no texto (numero de ouro, preco, "+25"): le sobre qualquer fundo.</summary>
        public static Text Outlined(Text t, float px)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f);
            o.effectDistance = new Vector2(px, -px);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0f, -px * 1.6f);
            return t;
        }

        public static Color StationColor(Station s)
        {
            switch (s.Kind)
            {
                case Kind.Deposit: return DepositC;
                case Kind.Furnace: return FurnaceC;
                case Kind.Counter: return CounterC;
                default: return s.OutItem == Item.Shield ? ShieldC : s.OutItem == Item.Tool ? ToolC : AnvilC;
            }
        }

        /// <summary>Mascara com 4 amostras de antialias por pixel (x, y em [-1, 1]).</summary>
        static Sprite Get(string key, Func<float, float, bool> inside) => Mask(key, (x, y) =>
        {
            int n = 0;
            for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (inside((x + 0.25f + sx * 0.5f) / Side * 2f - 1f, (y + 0.25f + sy * 0.5f) / Side * 2f - 1f)) n++;
            return (byte)(n * 63);
        });

        /// <summary>Degrade: alfa = f(x, y) em [0, 1], com x, y em [-1, 1] no centro do pixel.</summary>
        static Sprite Soft(string key, Func<float, float, float> f) =>
            Mask(key, (x, y) => (byte)(255f * Mathf.Clamp01(f((x + 0.5f) / Side * 2f - 1f, (y + 0.5f) / Side * 2f - 1f))));

        static Sprite Mask(string key, Func<int, int, byte> alpha)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            var tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Side * Side];
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++) px[y * Side + x] = new Color32(255, 255, 255, alpha(x, y));
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), Side);
            Cache[key] = s;
            return s;
        }

        /// <summary>Brilho de tocha/fornalha/poste: disco com queda suave (smoothstep invertido). v0.5b: era (1-d)^2, que sumia
        /// embaixo do sprite da fornalha a 0,6 m do centro; assim a poca de luz aparece no chao em volta.</summary>
        public static Sprite Glow() => Soft("glow2", (x, y) => { float d = Mathf.Min(1f, Mathf.Sqrt(x * x + y * y)); return (1f - d) * (1f - d) * (1f + 2f * d); });
        /// <summary>Sombra de contato: opaca na esquerda (x = -1), transparente na direita.</summary>
        public static Sprite Fade() => Soft("fade", (x, y) => (1f - x) * 0.5f);

        // ---------- superficies (chao/parede) em modo Tiled ----------
        public const float GroundTileM = 2f;   // 1 repeticao da textura a cada 2 m (coordenador, 2026-10-07; ajustar pela foto)

        /// <summary>
        /// Textura de chao/parede para SpriteRenderer Tiled: Resources/Textures/&lt;name&gt;.png (pintada pelo coordenador:
        /// piso_oficina, rua, parede, madeira) ou, sem o arquivo, blocos procedurais nas mesmas cores medias. Escurecer e
        /// face de parede sao tinta no renderer, igual para as duas fontes.
        /// </summary>
        public static Sprite Ground(string name)
        {
            string key = "chao:" + name;
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = Resources.Load<Texture2D>("Textures/" + name);
            if (tex == null) tex = name == "grama" ? Grass() : Blocks(name);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / GroundTileM, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }

        /// <summary>Reserva procedural de Ground: blocos w x h px, junta de 2 px, fiada desencontrada, 2 tons por hash do bloco.</summary>
        static Texture2D Blocks(string name)
        {
            const int n = 128, jp = 2, r = 4;   // 128 px = GroundTileM (64 px/m); calcamento com canto de 4 px
            int bw = 64, bh = 32; bool round = false;
            Color32 a = Hex(0x4E4539), b = Hex(0x463E34), j = Hex(0x2E2824);   // piso_oficina: lajes de pedra quente (ART_BIBLE s2)
            switch (name)
            {
                case "rua": bw = bh = 16; round = true; a = Hex(0x46525A); b = Hex(0x3C474F); j = Hex(0x262D35); break;
                case "parede": bw = 32; bh = 16; a = Hex(0x6B5F55); b = Hex(0x7A5048); j = Hex(0x3A322C); break;
                case "madeira": bh = 16; a = Hex(0x4A3220); b = Hex(0x402B1C); j = Hex(0x24180F); break;
            }
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int row = y / bh, sx = x + (row & 1) * bw / 2, col = sx / bw % (n / bw), lx = sx % bw, ly = y % bh;
                    bool joint = lx < jp || ly < jp;
                    if (round && !joint)
                    {
                        float qx = Mathf.Max(Mathf.Abs(lx + 0.5f - (bw + jp) * 0.5f) - ((bw - jp) * 0.5f - r), 0f);
                        float qy = Mathf.Max(Mathf.Abs(ly + 0.5f - (bh + jp) * 0.5f) - ((bh - jp) * 0.5f - r), 0f);
                        joint = qx * qx + qy * qy > r * r;
                    }
                    px[y * n + x] = joint ? j : (row * 7 + col * 13) % 3 == 0 ? b : a;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Grama do exterior (v0.5b, BENCHMARK P1-6 "fora da loja ha mundo"): ruido de valor em 2 oitavas sobre 3 verdes-oliva apagados
        /// (longe do verde #4CD964 da ferramenta) e tufos escuros esparsos; 128 px = GroundTileM, sem emenda (ruido periodico).
        /// </summary>
        static Texture2D Grass()
        {
            const int n = 128;
            Color a = Hex(0x5C7A44), b = Hex(0x6A8A4E), c = Hex(0x4E6A3A);
            float H(int x, int y) { uint h = (uint)((x & 127) * 374761393 + (y & 127) * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return (h & 0xFFFF) / 65535f; }
            float Noise(float x, float y, int cell)
            {
                int x0 = Mathf.FloorToInt(x / cell), y0 = Mathf.FloorToInt(y / cell), m = n / cell;
                float fx = x / cell - x0, fy = y / cell - y0;
                fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
                float v00 = H(x0 % m, y0 % m), v10 = H((x0 + 1) % m, y0 % m), v01 = H(x0 % m, (y0 + 1) % m), v11 = H((x0 + 1) % m, (y0 + 1) % m);
                return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy);
            }
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = 0.65f * Noise(x, y, 32) + 0.35f * Noise(x, y, 8);
                    Color col = v < 0.42f ? Color.Lerp(c, a, v / 0.42f) : Color.Lerp(a, b, (v - 0.42f) / 0.58f);
                    if (H(x * 7 + 3, y * 13 + 5) > 0.985f) col = Color.Lerp(col, c * 0.8f, 0.7f);   // tufo
                    px[y * n + x] = col;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "grama" };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Ladrilhos de 1 m (luxo Piso de oficina): xadrez `a`/`b` com junta na borda de baixo/esquerda; 1 repeticao = 2 x 2 m, desenhar Tiled.</summary>
        public static Sprite Tiles(Color a, Color b, Color joint)
        {
            const int ppm = 32, jointPx = 2, n = 2 * ppm;
            var px = new Color32[n * n];
            Color32 ca = a, cb = b, cj = joint;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = x % ppm < jointPx || y % ppm < jointPx ? cj : (x / ppm + y / ppm) % 2 == 0 ? ca : cb;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ppm, 0, SpriteMeshType.FullRect);
        }

        public static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Color color, int order,
            Vector2 pos, Vector2 scale, float angle = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        // ---------- uGUI por codigo (fonte embutida, sem TextMeshPro) ----------
        static Font _font;
        public static Font Font() => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Node(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Text NewText(Transform parent, string name, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Text>();
            t.font = Font();
            t.fontSize = size;
            t.color = Ink;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Texto solto (rotulo que segue um ponto do mundo, ou numero flutuante): sem ancora, tamanho fixo.</summary>
        public static Text FreeText(Transform parent, string name, int size, Vector2 sizePx)
        {
            Text t = NewText(parent, name, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            t.rectTransform.sizeDelta = sizePx;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var img = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            img.sprite = Box();   // v0.5b: 9-fatias, canto redondo de verdade (o Rounded esticado achatava os cantos)
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Button NewButton(Transform parent, string label, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            Image img = Panel(parent, "Btn " + label, color, anchorMin, anchorMax);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            Text t = NewText(img.transform, "Label", size, Vector2.zero, Vector2.one);
            t.text = label;
            t.color = Bg;
            t.fontStyle = FontStyle.Bold;
            return b;
        }
    }

    /// <summary>SFX sintetizados (padrao do Sintese.cs do COE): varredura de seno com decaimento, opcional ruido.</summary>
    public static class Sfx
    {
        static AudioSource _src;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>();

        public static void Init(GameObject host)
        {
            _src = host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            Tone("ore", 160, 70, 0.14f, 0.55f);        // pedra batendo
            Tone("furnace", 180, 420, 0.35f, 0.65f);   // sopro da fornalha
            Tone("hammer", 1500, 500, 0.09f, 0.25f);   // martelada
            Tone("coin", 1300, 2100, 0.14f);           // moeda
            Tone("upgrade", 420, 1250, 0.5f);          // compra
            Tone("hire", 520, 880, 0.35f, 0.1f);       // contratacao
            Tone("client", 980, 740, 0.16f);           // cliente chegou
            Tone("leave", 520, 230, 0.35f, 0.2f);      // cliente foi embora
            Tone("drop", 700, 500, 0.06f);             // item depositado
            Tone("offline", 523, 1046, 0.7f);          // cofre
        }

        /// <summary>Toca no maximo uma vez por `minGap` segundos por nome (varias marteladas no mesmo quadro viram uma).</summary>
        public static void Play(string name, float pitch = 1f, float minGap = 0.05f)
        {
            if (_src == null || !Clips.TryGetValue(name, out AudioClip c)) return;
            if (LastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < minGap) return;
            LastPlayed[name] = Time.unscaledTime;
            _src.pitch = pitch;
            _src.PlayOneShot(c, 0.55f);
        }

        static void Tone(string name, float f0, float f1, float dur, float noise = 0f)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += 2 * Math.PI * Mathf.Lerp(f0, f1, t) / rate;
                float env = Mathf.Min(1f, i / 200f) * Mathf.Exp(-4f * t);
                float v = (float)Math.Sin(phase) * (1f - noise) + ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = v * env * 0.5f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            Clips[name] = clip;
        }
    }
}
