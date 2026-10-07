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
        public static readonly Color Bg = Hex(0x121622), Floor = Hex(0x232A3B), FloorEdge = Hex(0x1A2030), Ink = Hex(0xEEF1F8),
            Accent = Hex(0xFFE08A), Bad = Hex(0xFF3B4E), Dim = Hex(0x4A5370), Pad = Hex(0x2C3550), PadFill = Hex(0x6B5F2E),
            Player = Hex(0xFFE08A), Worker = Hex(0x9ED0C8), Client = Hex(0xE8C9A8), Bubble = Hex(0xF4F6FA), Good = Hex(0x4CD964);
        /// <summary>Pedra fria da rua lateral (ART_BIBLE s2: base #55607A).</summary>
        public static readonly Color Street = Hex(0x3A4256);
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
        public static Sprite Rounded() => Get("rounded", (x, y) =>
        {
            const float r = 0.28f;
            float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
            return qx * qx + qy * qy <= r * r;
        });
        public static Sprite Disc() => Get("disc", (x, y) => x * x + y * y <= 0.96f);
        public static Sprite Ring() => Get("ring", (x, y) => { float d = x * x + y * y; return d <= 0.96f && d >= 0.62f; });
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

        /// <summary>Escala do sprite do item quando desenhado solto (lingote e' uma barra deitada).</summary>
        public static Vector2 ItemScale(Item i, float s) => i == Item.Ingot ? new Vector2(s * 1.25f, s * 0.6f) : new Vector2(s, s);

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

        static Sprite Get(string key, Func<float, float, bool> inside)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            var tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Side * Side];
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    int n = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            if (inside((x + 0.25f + sx * 0.5f) / Side * 2f - 1f, (y + 0.25f + sy * 0.5f) / Side * 2f - 1f)) n++;
                    px[y * Side + x] = new Color32(255, 255, 255, (byte)(n * 63));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), Side);
            Cache[key] = s;
            return s;
        }

        /// <summary>
        /// Piso de ladrilhos de 1 m (luxo Piso de oficina): xadrez `a`/`b` com junta `joint` na borda de baixo/esquerda de
        /// cada ladrilho, `cols` x `rows` m numa textura so (1 renderer). `inside(x, y)` em m a partir do canto de baixo-esquerdo
        /// recorta o contorno (fora = transparente). Pivo no centro.
        /// </summary>
        public static Sprite Tiles(int cols, int rows, Color a, Color b, Color joint, Func<float, float, bool> inside)
        {
            const int ppm = 32, jointPx = 2;   // ponytail: 32 px/m = ~3 px de tela por texel na camera de 11,4 m; ~0,5 MB (9 x 14 m), sem mipmap
            int w = cols * ppm, h = rows * ppm;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            Color32 ca = a, cb = b, cj = joint;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!inside((x + 0.5f) / ppm, (y + 0.5f) / ppm)) continue;   // Color32 padrao = transparente
                    px[y * w + x] = x % ppm < jointPx || y % ppm < jointPx ? cj : (x / ppm + y / ppm) % 2 == 0 ? ca : cb;
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppm);
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
            img.sprite = Rounded();
            img.type = Image.Type.Simple;
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
