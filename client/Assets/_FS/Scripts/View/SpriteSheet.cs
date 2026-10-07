using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace FS
{
    /// <summary>
    /// Folha pre-renderizada do Blender (tools/render_sprites.py, docs/RENDER_SPRITES.md): Resources/Sprites/&lt;nome&gt;/
    /// meta.json + 1 PNG por clipe, linha = direcao (linha 0 no TOPO do PNG, ordem do meta), coluna = quadro. Fatia em
    /// runtime e guarda cada Sprite na 1a vez que e' pedido. Sem folha (arquivo faltando, meta ruim, -nographics) o
    /// TryGet devolve false e a view desenha o procedural de sempre.
    /// </summary>
    public sealed class SpriteSheet
    {
        [Serializable] public sealed class ClipMeta { public string name, file; public int frames; public bool loop; public float fps; }
        [Serializable] public sealed class Meta { public int size, dirs; public string[] dir_order; public float ppu; public float[] pivot; public List<ClipMeta> clips; }

        static readonly Dictionary<string, SpriteSheet> Cache = new Dictionary<string, SpriteSheet>();
        static readonly string[] Compass4 = { "S", "W", "N", "E" }, Compass8 = { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };

        public readonly int Size, Dirs;
        public readonly float Ppu;
        /// <summary>pixelsPerUnit = Ppu * Scale. Quem consome define antes do 1o Frame (1 uso por nome de folha).</summary>
        public float Scale = 1f;

        readonly Meta _m;
        readonly Texture2D[] _tex;
        readonly Sprite[][] _spr;
        readonly int[] _row;   // indice da bussola (horario a partir de S) -> linha da folha

        SpriteSheet(Meta m, Texture2D[] tex)
        {
            _m = m; _tex = tex; _spr = new Sprite[tex.Length][];
            Size = m.size; Dirs = m.dirs; Ppu = m.ppu;
            string[] compass = Dirs == 8 ? Compass8 : Dirs == 4 ? Compass4 : null;
            _row = new int[Dirs];
            for (int k = 0; k < Dirs; k++)
            {
                int i = compass != null && m.dir_order != null ? Array.IndexOf(m.dir_order, compass[k]) : -1;
                _row[k] = i >= 0 ? i : k;
            }
        }

        /// <summary>Carrega (1 vez por nome, inclusive a falha) e devolve a folha.</summary>
        public static bool TryGet(string name, out SpriteSheet sheet)
        {
            if (!Cache.TryGetValue(name, out sheet)) Cache[name] = sheet = Load(name);
            return sheet != null;
        }

        static SpriteSheet Load(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return null;   // -batchmode -nographics: procedural
            var json = Resources.Load<TextAsset>("Sprites/" + name + "/meta");
            if (json == null) return null;
            Meta m;
            try { m = JsonUtility.FromJson<Meta>(json.text); }
            catch (ArgumentException) { m = null; }
            if (m == null || m.size <= 0 || m.dirs <= 0 || m.ppu <= 0f || m.clips == null || m.clips.Count == 0)
            {
                Debug.LogWarning("SpriteSheet " + name + ": meta.json invalido, usando procedural");
                return null;
            }
            var tex = new Texture2D[m.clips.Count];
            for (int i = 0; i < tex.Length; i++)
            {
                ClipMeta c = m.clips[i];
                string file = string.IsNullOrEmpty(c.file) ? c.name : Path.GetFileNameWithoutExtension(c.file);
                tex[i] = Resources.Load<Texture2D>("Sprites/" + name + "/" + file);
                if (tex[i] == null || c.frames <= 0 || tex[i].width < c.frames || tex[i].height < m.dirs)
                {
                    Debug.LogWarning("SpriteSheet " + name + ": clipe " + c.name + " sem PNG ou fora do meta, usando procedural");
                    return null;
                }
            }
            return new SpriteSheet(m, tex);
        }

        int Index(string clip)
        {
            for (int i = 0; i < _m.clips.Count; i++) if (_m.clips[i].name == clip) return i;
            return -1;
        }

        public bool Has(string clip) => Index(clip) >= 0;

        /// <summary>Duracao de 1 passada do clipe, em s (0 se a folha nao tem o clipe).</summary>
        public float Length(string clip)
        {
            int i = Index(clip);
            return i < 0 ? 0f : _m.clips[i].frames / Fps(_m.clips[i]);
        }

        static float Fps(ClipMeta c) => c.fps > 0f ? c.fps : 12f;   // fps POR CLIPE (o Idle do ferreiro e' 3,2; o global e' so a amostragem)

        /// <summary>
        /// Quadro do clipe na linha `dir` no tempo `t` (s desde o inicio do clipe). Laco pelo `loop` do meta (ou `forceLoop`);
        /// sem laco para no ultimo quadro. Clipe que a folha nao tem cai no Idle (ou no 1o clipe).
        /// </summary>
        public Sprite Frame(string clip, int dir, float t, bool forceLoop = false)
        {
            int ci = Index(clip);
            if (ci < 0) ci = Math.Max(0, Index("Idle"));
            ClipMeta c = _m.clips[ci];
            int n = c.frames, f = Mathf.Max(0, (int)(t * Fps(c)));
            f = c.loop || forceLoop ? f % n : Mathf.Min(f, n - 1);
            dir = Mathf.Clamp(dir, 0, Dirs - 1);
            Sprite[] arr = _spr[ci] ?? (_spr[ci] = new Sprite[Dirs * n]);
            int k = dir * n + f;
            if (arr[k] != null) return arr[k];
            // celula pela textura real: se o import reduziu a folha (maxTextureSize), recorte e ppu acompanham
            Texture2D tx = _tex[ci];
            float cw = tx.width / (float)n, ch = tx.height / (float)Dirs;
            var rect = new Rect(f * cw, (Dirs - 1 - dir) * ch, cw, ch);
            Vector2 pivot = _m.pivot != null && _m.pivot.Length >= 2 ? new Vector2(_m.pivot[0], _m.pivot[1]) : new Vector2(0.5f, 0f);
            return arr[k] = Sprite.Create(tx, rect, pivot, Ppu * Scale * cw / Size, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Linha da folha para a velocidade `v` (XY do mundo, +Y = para cima na tela, S = para baixo); parado devolve `last`.</summary>
        public int Dir(Vector2 v, int last)
        {
            if (Dirs == 1) return 0;
            if (v.sqrMagnitude < 1e-6f) return last;
            // ponytail: sem histerese; joystick parado perto de 45 graus pode alternar S/W. Adicionar se o video mostrar
            int k = (Mathf.RoundToInt(-Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg / (360f / Dirs)) % Dirs + Dirs) % Dirs;
            return _row[k];
        }
    }
}
