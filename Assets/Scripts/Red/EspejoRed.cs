using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Campos de un registro del espejo (máscara de bits).</summary>
internal static class CampoEspejo
{
    public const ushort Posicion = 1, Rotacion = 2, Escala = 4, Sprite = 8, Color = 16, Volteo = 32, Orden = 64,
        Tamano = 128, Material = 256, Texto = 512, Fuente = 1024, Nuevo = 32768;
    public const byte TipoSprite = 0, TipoTexto = 1;
}

/// <summary>
/// ANFITRIÓN: cada 1/25 s junta todo lo que se dibuja en el mundo (sprites y textos) y le manda al invitado solo lo que
/// cambió. Lo del mapa (paredes, decoración, objetos reparables, "?") no viaja: el invitado lo arma igual y solo recibe
/// el estado (roto, dañado...). Los sprites viajan por nombre y, si el invitado no los tiene cargados, como PNG.
/// </summary>
public class EspejoAnfitrion : MonoBehaviour
{
    public static EspejoAnfitrion Actual { get; private set; }
    [System.NonSerialized] public bool invitadoListo;

    private const float Periodo = 1f / 25f;
    private float proximo;

    private class Reg
    {
        public int id;
        public bool decidido, incluir, enviado, visto;
        public Vector3 pos;
        public float rot;
        public Vector2 esc;
        public int sprite = -1;
        public Color32 color;
        public byte volteo, modo;
        public int capa, orden;
        public Vector2 tam;
        public Material mat;
        public string texto;
        public float tamCaracter, interlineado;
        public int tamFuente;
        public byte ancla, alineacion, estilo;
        public Font fuente;
    }

    private readonly Dictionary<SpriteRenderer, Reg> sprites = new Dictionary<SpriteRenderer, Reg>();
    private readonly Dictionary<TextMesh, Reg> textos = new Dictionary<TextMesh, Reg>();
    private readonly Dictionary<TextMesh, MeshRenderer> mallas = new Dictionary<TextMesh, MeshRenderer>();
    private readonly Dictionary<Sprite, int> idsSprite = new Dictionary<Sprite, int>();
    private readonly List<Sprite> spritePorId = new List<Sprite>();
    private readonly List<int> bajas = new List<int>();
    private readonly List<SpriteRenderer> muertosS = new List<SpriteRenderer>();
    private readonly List<TextMesh> muertosT = new List<TextMesh>();
    private int siguienteId = 1;
    private Transform raizMapa;

    private readonly Dictionary<string, Vector2> bucles = new Dictionary<string, Vector2>();   // clave → (activo, volumen)
    private readonly Dictionary<string, string> nombresBucle = new Dictionary<string, string>();

    private MemoryStream cuerpo = new MemoryStream(1 << 15);
    private BinaryWriter w;

    private void Awake()
    {
        Actual = this;
        w = new BinaryWriter(cuerpo, System.Text.Encoding.UTF8);
        spritePorId.Add(null);   // el 0 = sin sprite
    }

    private void OnEnable()
    {
        GestorAudio.AlReproducir += AlSonido;
        GestorAudio.AlReproducirClip += AlClip;
        GestorAudio.AlBucle += AlBucle;
        CamaraPisoCenital.AlTemblar += AlTemblar;
        AvisoUlti.AlMostrar += AlAvisoUlti;
        AvisoUlti.AlOcultar += AlOcultarUlti;
    }

    private void OnDisable()
    {
        GestorAudio.AlReproducir -= AlSonido;
        GestorAudio.AlReproducirClip -= AlClip;
        GestorAudio.AlBucle -= AlBucle;
        CamaraPisoCenital.AlTemblar -= AlTemblar;
        AvisoUlti.AlMostrar -= AlAvisoUlti;
        AvisoUlti.AlOcultar -= AlOcultarUlti;
        if (Actual == this) Actual = null;
    }

    private bool Activo { get { return invitadoListo && RedJuego.EsAnfitrionEnPartida; } }

    // ------------------------------------------------------------ eventos (sonidos, temblores, avisos)
    private void AlSonido(string nombre, float vol, float tono)
    {
        if (!Activo || string.IsNullOrEmpty(nombre)) return;
        RedJuego.Enviar(MsgRed.Evento, x => { x.Write(EvRed.Sonido); x.Write(nombre); x.Write(vol); x.Write(tono); });
    }

    private void AlClip(string clave, AudioClip clip, float vol)
    {
        if (!Activo || clip == null) return;
        RedJuego.Enviar(MsgRed.Evento, x => { x.Write(EvRed.Clip); x.Write(clave ?? ""); x.Write(clip.name ?? ""); x.Write(vol); });
    }

    private void AlBucle(string clave, string nombre, bool activo, float vol)
    {
        if (!Activo || string.IsNullOrEmpty(clave)) return;
        Vector2 previo;
        bool hay = bucles.TryGetValue(clave, out previo);
        float a = activo ? 1f : 0f;
        if (hay && previo.x == a && (!activo || Mathf.Abs(previo.y - vol) < 0.1f)) return;
        if (!hay && !activo) { bucles[clave] = new Vector2(0f, vol); return; }
        bucles[clave] = new Vector2(a, vol);
        nombresBucle[clave] = nombre;
        RedJuego.Enviar(MsgRed.Evento, x => { x.Write(EvRed.Bucle); x.Write(clave); x.Write(nombre ?? ""); x.Write(activo); x.Write(vol); });
    }

    private void AlTemblar(float fuerza, float duracion)
    {
        if (!Activo) return;
        RedJuego.Enviar(MsgRed.Evento, x => { x.Write(EvRed.Temblor); x.Write(fuerza); x.Write(duracion); });
    }

    private void AlAvisoUlti(string mensaje, float segundos, bool bordes, Color c)
    {
        if (!Activo) return;
        RedJuego.Enviar(MsgRed.Evento, x =>
        {
            x.Write(EvRed.AvisoUlti); x.Write(mensaje ?? ""); x.Write(segundos); x.Write(bordes);
            x.Write(c.r); x.Write(c.g); x.Write(c.b);
        });
    }

    private void AlOcultarUlti()
    {
        if (!Activo) return;
        RedJuego.Enviar(MsgRed.Evento, x => x.Write(EvRed.OcultarUlti));
    }

    // ------------------------------------------------------------ captura
    private void LateUpdate()
    {
        if (!Activo) return;
        if (Time.unscaledTime < proximo) return;
        proximo = Time.unscaledTime + Periodo;
        if (raizMapa == null && PisoCenital.Actual != null && PisoCenital.Actual.Mapa != null) raizMapa = PisoCenital.Actual.Mapa.transform;

        cuerpo.SetLength(0);
        MultijugadorNivel.EscribirHud(w);

        // Cambios: primero se cuentan en otro búfer.
        long posCuenta = cuerpo.Position;
        w.Write((ushort)0);
        int cambios = 0;
        bajas.Clear();

        foreach (Reg r in sprites.Values) r.visto = false;
        foreach (Reg r in textos.Values) r.visto = false;

        SpriteRenderer[] srs = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
        for (int i = 0; i < srs.Length; i++)
        {
            SpriteRenderer sr = srs[i];
            Reg r;
            if (!sprites.TryGetValue(sr, out r)) { r = new Reg { id = siguienteId++ }; sprites[sr] = r; }
            if (!r.decidido) { r.incluir = Incluir(sr.transform); r.decidido = true; }
            if (!r.incluir) continue;
            if (!sr.enabled || sr.sprite == null || sr.color.a <= 0.003f) continue;
            r.visto = true;
            if (EscribirSprite(sr, r)) cambios++;
        }

        TextMesh[] tms = FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude);
        for (int i = 0; i < tms.Length; i++)
        {
            TextMesh tm = tms[i];
            Reg r;
            if (!textos.TryGetValue(tm, out r)) { r = new Reg { id = siguienteId++ }; textos[tm] = r; }
            if (!r.decidido) { r.incluir = Incluir(tm.transform); r.decidido = true; }
            if (!r.incluir) continue;
            MeshRenderer mr;
            if (!mallas.TryGetValue(tm, out mr)) { mr = tm.GetComponent<MeshRenderer>(); mallas[tm] = mr; }
            if (mr == null || !mr.enabled || string.IsNullOrEmpty(tm.text)) continue;
            r.visto = true;
            if (EscribirTexto(tm, mr, r)) cambios++;
        }

        // Lo que dejó de verse (o se destruyó) se da de baja.
        muertosS.Clear();
        foreach (KeyValuePair<SpriteRenderer, Reg> kv in sprites)
        {
            if (kv.Value.enviado && !kv.Value.visto) { bajas.Add(kv.Value.id); kv.Value.enviado = false; }
            if (kv.Key == null) muertosS.Add(kv.Key);
        }
        foreach (SpriteRenderer m in muertosS) sprites.Remove(m);
        muertosT.Clear();
        foreach (KeyValuePair<TextMesh, Reg> kv in textos)
        {
            if (kv.Value.enviado && !kv.Value.visto) { bajas.Add(kv.Value.id); kv.Value.enviado = false; }
            if (kv.Key == null) muertosT.Add(kv.Key);
        }
        foreach (TextMesh m in muertosT) { textos.Remove(m); mallas.Remove(m); }

        w.Write((ushort)bajas.Count);
        foreach (int id in bajas) w.Write(id);
        w.Flush();

        long fin = cuerpo.Position;
        cuerpo.Position = posCuenta;
        w.Write((ushort)Mathf.Min(cambios, 65535));
        w.Flush();
        cuerpo.Position = fin;

        byte[] datos = cuerpo.ToArray();
        RedJuego.Enviar(MsgRed.Mundo, x => x.Write(datos));
    }

    /// <summary>¿Este dibujo viaja por la red? Los personajes, enemigos y efectos sí; el mapa no (el invitado lo tiene).</summary>
    /// <summary>Un sprite reciclado (partículas y efectos reutilizados) puede cambiar de padre: se vuelve a decidir si viaja.</summary>
    public static void Redecidir(SpriteRenderer sr)
    {
        if (Actual == null || sr == null) return;
        Reg r;
        if (Actual.sprites.TryGetValue(sr, out r)) r.decidido = false;
    }

    private bool Incluir(Transform t)
    {
        if (t.GetComponentInParent<JugadorCenital>(true) != null || t.GetComponentInParent<DemoledorCenital>(true) != null ||
            t.GetComponentInParent<EstudianteNPC>(true) != null || t.GetComponentInParent<MiniVillano>(true) != null)
            return true;
        if (raizMapa != null && t.IsChildOf(raizMapa)) return false;
        if (t.GetComponentInParent<AmbienteNivel>(true) != null || t.GetComponentInParent<Camera>(true) != null) return false;
        if (t.GetComponentInParent<Canvas>(true) != null) return false;
        return true;
    }

    private static bool Distinto(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude > 0.0000025f; }
    private static bool Distinto(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude > 0.0000025f; }
    private static bool Distinto(Color32 a, Color32 b) { return a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a; }

    private bool EscribirSprite(SpriteRenderer sr, Reg r)
    {
        Transform t = sr.transform;
        Vector3 pos = t.position;
        float rot = t.eulerAngles.z;
        Vector3 l = t.lossyScale;
        Vector2 esc = new Vector2(l.x, l.y);
        int sp = IdSprite(sr.sprite);
        Color32 col = sr.color;
        byte volteo = (byte)((sr.flipX ? 1 : 0) | (sr.flipY ? 2 : 0));
        int capa = sr.sortingLayerID, orden = sr.sortingOrder;
        byte modo = (byte)sr.drawMode;
        Vector2 tam = modo != 0 ? sr.size : Vector2.zero;
        Material mat = sr.sharedMaterial;

        ushort m = 0;
        bool nuevo = !r.enviado;
        if (nuevo) m = (ushort)(CampoEspejo.Nuevo | CampoEspejo.Posicion | CampoEspejo.Rotacion | CampoEspejo.Escala | CampoEspejo.Sprite |
                                CampoEspejo.Color | CampoEspejo.Volteo | CampoEspejo.Orden | CampoEspejo.Tamano | CampoEspejo.Material);
        else
        {
            if (Distinto(pos, r.pos)) m |= CampoEspejo.Posicion;
            if (Mathf.Abs(Mathf.DeltaAngle(rot, r.rot)) > 0.2f) m |= CampoEspejo.Rotacion;
            if (Distinto(esc, r.esc)) m |= CampoEspejo.Escala;
            if (sp != r.sprite) m |= CampoEspejo.Sprite;
            if (Distinto(col, r.color)) m |= CampoEspejo.Color;
            if (volteo != r.volteo) m |= CampoEspejo.Volteo;
            if (capa != r.capa || orden != r.orden) m |= CampoEspejo.Orden;
            if (modo != r.modo || Distinto(tam, r.tam)) m |= CampoEspejo.Tamano;
            if (mat != r.mat) m |= CampoEspejo.Material;
        }
        if (m == 0) return false;

        w.Write(r.id);
        w.Write(m);
        if ((m & CampoEspejo.Nuevo) != 0) w.Write(CampoEspejo.TipoSprite);
        if ((m & CampoEspejo.Posicion) != 0) { w.Write(pos.x); w.Write(pos.y); w.Write(pos.z); r.pos = pos; }
        if ((m & CampoEspejo.Rotacion) != 0) { w.Write(rot); r.rot = rot; }
        if ((m & CampoEspejo.Escala) != 0) { w.Write(esc.x); w.Write(esc.y); r.esc = esc; }
        if ((m & CampoEspejo.Sprite) != 0) { w.Write(sp); r.sprite = sp; }
        if ((m & CampoEspejo.Color) != 0) { w.Write(col.r); w.Write(col.g); w.Write(col.b); w.Write(col.a); r.color = col; }
        if ((m & CampoEspejo.Volteo) != 0) { w.Write(volteo); r.volteo = volteo; }
        if ((m & CampoEspejo.Orden) != 0) { w.Write(capa); w.Write(orden); r.capa = capa; r.orden = orden; }
        if ((m & CampoEspejo.Tamano) != 0) { w.Write(modo); w.Write(tam.x); w.Write(tam.y); r.modo = modo; r.tam = tam; }
        if ((m & CampoEspejo.Material) != 0) { w.Write(mat != null ? mat.name : ""); r.mat = mat; }
        r.enviado = true;
        return true;
    }

    private bool EscribirTexto(TextMesh tm, MeshRenderer mr, Reg r)
    {
        Transform t = tm.transform;
        Vector3 pos = t.position;
        float rot = t.eulerAngles.z;
        Vector3 l = t.lossyScale;
        Vector2 esc = new Vector2(l.x, l.y);
        Color32 col = tm.color;
        int capa = mr.sortingLayerID, orden = mr.sortingOrder;

        ushort m = 0;
        bool nuevo = !r.enviado;
        if (nuevo) m = (ushort)(CampoEspejo.Nuevo | CampoEspejo.Posicion | CampoEspejo.Rotacion | CampoEspejo.Escala | CampoEspejo.Color |
                                CampoEspejo.Orden | CampoEspejo.Texto | CampoEspejo.Fuente);
        else
        {
            if (Distinto(pos, r.pos)) m |= CampoEspejo.Posicion;
            if (Mathf.Abs(Mathf.DeltaAngle(rot, r.rot)) > 0.2f) m |= CampoEspejo.Rotacion;
            if (Distinto(esc, r.esc)) m |= CampoEspejo.Escala;
            if (Distinto(col, r.color)) m |= CampoEspejo.Color;
            if (capa != r.capa || orden != r.orden) m |= CampoEspejo.Orden;
            if (tm.text != r.texto) m |= CampoEspejo.Texto;
            if (tm.font != r.fuente || tm.fontSize != r.tamFuente || Mathf.Abs(tm.characterSize - r.tamCaracter) > 0.0001f ||
                (byte)tm.anchor != r.ancla || (byte)tm.alignment != r.alineacion || (byte)tm.fontStyle != r.estilo ||
                Mathf.Abs(tm.lineSpacing - r.interlineado) > 0.001f) m |= CampoEspejo.Fuente;
        }
        if (m == 0) return false;

        w.Write(r.id);
        w.Write(m);
        if ((m & CampoEspejo.Nuevo) != 0) w.Write(CampoEspejo.TipoTexto);
        if ((m & CampoEspejo.Posicion) != 0) { w.Write(pos.x); w.Write(pos.y); w.Write(pos.z); r.pos = pos; }
        if ((m & CampoEspejo.Rotacion) != 0) { w.Write(rot); r.rot = rot; }
        if ((m & CampoEspejo.Escala) != 0) { w.Write(esc.x); w.Write(esc.y); r.esc = esc; }
        if ((m & CampoEspejo.Color) != 0) { w.Write(col.r); w.Write(col.g); w.Write(col.b); w.Write(col.a); r.color = col; }
        if ((m & CampoEspejo.Orden) != 0) { w.Write(capa); w.Write(orden); r.capa = capa; r.orden = orden; }
        if ((m & CampoEspejo.Texto) != 0) { w.Write(tm.text ?? ""); r.texto = tm.text; }
        if ((m & CampoEspejo.Fuente) != 0)
        {
            w.Write(tm.font != null ? tm.font.name : "");
            w.Write(tm.fontSize); w.Write(tm.characterSize); w.Write((byte)tm.anchor); w.Write((byte)tm.alignment);
            w.Write((byte)tm.fontStyle); w.Write(tm.lineSpacing);
            r.fuente = tm.font; r.tamFuente = tm.fontSize; r.tamCaracter = tm.characterSize; r.ancla = (byte)tm.anchor;
            r.alineacion = (byte)tm.alignment; r.estilo = (byte)tm.fontStyle; r.interlineado = tm.lineSpacing;
        }
        r.enviado = true;
        return true;
    }

    // ------------------------------------------------------------ sprites
    private int IdSprite(Sprite s)
    {
        if (s == null) return 0;
        int id;
        if (idsSprite.TryGetValue(s, out id)) return id;
        id = spritePorId.Count;
        spritePorId.Add(s);
        idsSprite[s] = id;
        Texture2D tex = s.texture;
        string nombre = s.name ?? "";
        string nombreTex = tex != null ? tex.name : "";
        bool forzarPng = string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(nombreTex) || (tex != null && tex.isReadable && nombre.StartsWith("Provisional"));
        Rect rr = s.rect;
        Vector2 piv = s.pivot;
        Vector4 borde = s.border;
        float ppu = s.pixelsPerUnit;
        string ruta = ArteExtra.RutaDe(s) ?? "";
        RedJuego.Enviar(MsgRed.SpriteDef, x =>
        {
            x.Write(id); x.Write(ruta); x.Write(nombre); x.Write(nombreTex);
            x.Write(rr.x); x.Write(rr.y); x.Write(rr.width); x.Write(rr.height);
            x.Write(piv.x); x.Write(piv.y); x.Write(ppu);
            x.Write(borde.x); x.Write(borde.y); x.Write(borde.z); x.Write(borde.w);
            x.Write(forzarPng);
        });
        return id;
    }

    /// <summary>El invitado no tiene este sprite: se lo manda como PNG.</summary>
    public void EnviarPng(int id)
    {
        if (id <= 0 || id >= spritePorId.Count) return;
        Sprite s = spritePorId[id];
        if (s == null || s.texture == null) return;
        byte[] png = CodificarSprite(s);
        if (png == null) return;
        Rect rr = s.rect;
        Vector2 piv = s.pivot;
        Vector4 borde = s.border;
        float ppu = s.pixelsPerUnit;
        RedJuego.Enviar(MsgRed.SpritePng, x =>
        {
            x.Write(id); x.Write(png.Length); x.Write(png);
            x.Write(rr.width); x.Write(rr.height); x.Write(piv.x); x.Write(piv.y); x.Write(ppu);
            x.Write(borde.x); x.Write(borde.y); x.Write(borde.z); x.Write(borde.w);
        });
    }

    private static byte[] CodificarSprite(Sprite s)
    {
        Texture2D tex = s.texture;
        Rect r = s.rect;
        try
        {
            if (s.packed && s.packingMode == SpritePackingMode.Rectangle) r = s.textureRect;
        }
        catch { }
        int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y), w = Mathf.Max(1, Mathf.RoundToInt(r.width)), h = Mathf.Max(1, Mathf.RoundToInt(r.height));
        Texture2D salida = new Texture2D(w, h, TextureFormat.RGBA32, false);
        try
        {
            bool leido = false;
            if (tex.isReadable)
            {
                try { salida.SetPixels(tex.GetPixels(x, y, w, h)); leido = true; }
                catch { leido = false; }
            }
            if (!leido)
            {
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(tex, rt);
                RenderTexture previo = RenderTexture.active;
                RenderTexture.active = rt;
                salida.ReadPixels(new Rect(x, y, w, h), 0, 0);
                RenderTexture.active = previo;
                RenderTexture.ReleaseTemporary(rt);
            }
            salida.Apply();
            return salida.EncodeToPNG();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Red] No se pudo copiar el sprite " + s.name + ": " + e.Message);
            return null;
        }
        finally { Destroy(salida); }
    }
}

/// <summary>
/// INVITADO: dibuja lo que manda el anfitrión (marionetas con SpriteRenderer o TextMesh) con movimiento suavizado, y
/// pide como PNG los sprites que no tiene cargados.
/// </summary>
public class EspejoInvitado : MonoBehaviour
{
    public static EspejoInvitado Actual { get; private set; }

    private class Marioneta
    {
        public GameObject go;
        public SpriteRenderer sr;
        public TextMesh tm;
        public MeshRenderer mr;
        public Vector3 objetivo;
        public int sprite;
    }

    private class DefSprite
    {
        public string nombre, textura;
        public Rect rect;
        public Vector2 pivote;
        public float ppu;
        public Vector4 borde;
        public Sprite sprite;
        public bool pedido;
    }

    private readonly Dictionary<int, Marioneta> marionetas = new Dictionary<int, Marioneta>();
    private readonly Dictionary<int, DefSprite> defs = new Dictionary<int, DefSprite>();
    private Dictionary<string, Sprite> indice;
    private float ultimoIndice = -10f;
    private readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();
    private readonly Dictionary<string, Font> fuentes = new Dictionary<string, Font>();
    private readonly List<Texture2D> texturasPropias = new List<Texture2D>();
    private Material materialBase;

    private void Awake()
    {
        Actual = this;
    }

    private void OnDestroy()
    {
        if (Actual == this) Actual = null;
        foreach (Texture2D t in texturasPropias) if (t != null) Destroy(t);
    }

    /// <summary>Raíz de las marionetas (el invitado no las oculta).</summary>
    public Transform Raiz { get { return transform; } }

    // ------------------------------------------------------------ mensajes
    public void Procesar(byte tipo, BinaryReader r)
    {
        switch (tipo)
        {
            case MsgRed.SpriteDef: LeerDef(r); break;
            case MsgRed.SpritePng: LeerPng(r); break;
            case MsgRed.Mundo: LeerMundo(r); break;
        }
    }

    private static string Clave(string nombre, string tex, Rect r)
    {
        return nombre + "|" + tex + "|" + Mathf.RoundToInt(r.x) + "," + Mathf.RoundToInt(r.y) + "," + Mathf.RoundToInt(r.width) + "," + Mathf.RoundToInt(r.height);
    }

    private void LeerDef(BinaryReader r)
    {
        DefSprite d = new DefSprite();
        int id = r.ReadInt32();
        string ruta = r.ReadString();
        d.nombre = r.ReadString();
        d.textura = r.ReadString();
        d.rect = new Rect(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        d.pivote = new Vector2(r.ReadSingle(), r.ReadSingle());
        d.ppu = r.ReadSingle();
        d.borde = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        bool forzarPng = r.ReadBoolean();
        defs[id] = d;
        if (!string.IsNullOrEmpty(ruta)) d.sprite = ArteExtra.Uno(ruta);   // exacto: la misma ruta de Resources
        if (d.sprite == null && !forzarPng) d.sprite = Buscar(d);
        if (d.sprite == null) Pedir(id, d);
    }

    private Sprite Buscar(DefSprite d)
    {
        string clave = Clave(d.nombre, d.textura, d.rect);
        Sprite s;
        if (indice != null && indice.TryGetValue(clave, out s))
        {
            if (s != null) return s;
            return null;   // ambiguo: se pide el PNG
        }
        if (Time.unscaledTime - ultimoIndice < 1.5f && indice != null) return null;
        // Reconstruye el índice con todo lo cargado en memoria (los sprites del piso y de Resources ya usados).
        ultimoIndice = Time.unscaledTime;
        indice = new Dictionary<string, Sprite>();
        foreach (Sprite x in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (x == null || x.texture == null || string.IsNullOrEmpty(x.name)) continue;
            string k = Clave(x.name, x.texture.name, x.rect);
            Sprite previo;
            if (!indice.TryGetValue(k, out previo)) indice[k] = x;
            else if (previo != null && previo != x) indice[k] = null;   // nombre repetido: no se adivina, se pide el PNG
        }
        return indice.TryGetValue(clave, out s) ? s : null;
    }

    private void Pedir(int id, DefSprite d)
    {
        if (d.pedido) return;
        d.pedido = true;
        RedJuego.Enviar(MsgRed.PedirSprite, x => x.Write(id));
    }

    private void LeerPng(BinaryReader r)
    {
        int id = r.ReadInt32();
        int largo = r.ReadInt32();
        byte[] png = r.ReadBytes(largo);
        float w = r.ReadSingle(), h = r.ReadSingle();
        Vector2 piv = new Vector2(r.ReadSingle(), r.ReadSingle());
        float ppu = r.ReadSingle();
        Vector4 borde = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!t.LoadImage(png)) { Destroy(t); return; }
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        texturasPropias.Add(t);
        Vector2 pivNorm = new Vector2(w > 0f ? piv.x / w : 0.5f, h > 0f ? piv.y / h : 0.5f);
        Sprite s = Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), pivNorm, Mathf.Max(1f, ppu), 0, SpriteMeshType.FullRect, borde);
        DefSprite d;
        if (!defs.TryGetValue(id, out d)) { d = new DefSprite(); defs[id] = d; }
        d.sprite = s;
        foreach (Marioneta m in marionetas.Values) if (m.sr != null && m.sprite == id) m.sr.sprite = s;
    }

    private Sprite SpriteDe(int id)
    {
        if (id <= 0) return null;
        DefSprite d;
        if (!defs.TryGetValue(id, out d)) return null;
        if (d.sprite == null && !d.pedido) { d.sprite = Buscar(d); if (d.sprite == null) Pedir(id, d); }
        return d.sprite;
    }

    private void LeerMundo(BinaryReader r)
    {
        MultijugadorNivel.LeerHud(r);
        int n = r.ReadUInt16();
        for (int i = 0; i < n; i++)
        {
            int id = r.ReadInt32();
            ushort m = r.ReadUInt16();
            Marioneta mar;
            bool nuevo = (m & CampoEspejo.Nuevo) != 0;
            if (nuevo)
            {
                byte tipo = r.ReadByte();
                if (marionetas.TryGetValue(id, out mar)) { Destroy(mar.go); marionetas.Remove(id); }
                mar = Crear(id, tipo);
            }
            else if (!marionetas.TryGetValue(id, out mar)) mar = null;
            Aplicar(r, m, mar, nuevo);
        }
        int bajas = r.ReadUInt16();
        for (int i = 0; i < bajas; i++)
        {
            int id = r.ReadInt32();
            Marioneta mar;
            if (marionetas.TryGetValue(id, out mar)) { Destroy(mar.go); marionetas.Remove(id); }
        }
    }

    private Marioneta Crear(int id, byte tipo)
    {
        Marioneta mar = new Marioneta();
        mar.go = new GameObject(tipo == CampoEspejo.TipoTexto ? "Texto" : "Sprite");
        mar.go.transform.SetParent(transform, false);
        if (tipo == CampoEspejo.TipoTexto)
        {
            mar.tm = mar.go.AddComponent<TextMesh>();
            mar.mr = mar.go.GetComponent<MeshRenderer>();
        }
        else
        {
            mar.sr = mar.go.AddComponent<SpriteRenderer>();
            if (materialBase == null) materialBase = mar.sr.sharedMaterial;
        }
        marionetas[id] = mar;
        return mar;
    }

    /// <summary>Lee un registro; con 'mar' null solo lo salta (un cambio de algo que ya no está).</summary>
    private void Aplicar(BinaryReader r, ushort m, Marioneta mar, bool nuevo)
    {
        Transform t = mar != null ? mar.go.transform : null;
        if ((m & CampoEspejo.Posicion) != 0)
        {
            Vector3 p = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            if (mar != null)
            {
                mar.objetivo = p;
                if (nuevo || (t.position - p).sqrMagnitude > 9f) t.position = p;
            }
        }
        if ((m & CampoEspejo.Rotacion) != 0) { float z = r.ReadSingle(); if (t != null) t.rotation = Quaternion.Euler(0f, 0f, z); }
        if ((m & CampoEspejo.Escala) != 0) { Vector2 e = new Vector2(r.ReadSingle(), r.ReadSingle()); if (t != null) t.localScale = new Vector3(e.x, e.y, 1f); }
        if ((m & CampoEspejo.Sprite) != 0)
        {
            int sp = r.ReadInt32();
            if (mar != null && mar.sr != null) { mar.sprite = sp; mar.sr.sprite = SpriteDe(sp); }
        }
        if ((m & CampoEspejo.Color) != 0)
        {
            Color32 c = new Color32(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
            if (mar != null) { if (mar.sr != null) mar.sr.color = c; if (mar.tm != null) mar.tm.color = c; }
        }
        if ((m & CampoEspejo.Volteo) != 0)
        {
            byte v = r.ReadByte();
            if (mar != null && mar.sr != null) { mar.sr.flipX = (v & 1) != 0; mar.sr.flipY = (v & 2) != 0; }
        }
        if ((m & CampoEspejo.Orden) != 0)
        {
            int capa = r.ReadInt32(), orden = r.ReadInt32();
            if (mar != null)
            {
                Renderer rd = mar.sr != null ? (Renderer)mar.sr : mar.mr;
                if (rd != null) { rd.sortingLayerID = capa; rd.sortingOrder = orden; }
            }
        }
        if ((m & CampoEspejo.Tamano) != 0)
        {
            byte modo = r.ReadByte();
            Vector2 tam = new Vector2(r.ReadSingle(), r.ReadSingle());
            if (mar != null && mar.sr != null)
            {
                mar.sr.drawMode = (SpriteDrawMode)modo;
                if (modo != 0) mar.sr.size = tam;
            }
        }
        if ((m & CampoEspejo.Material) != 0)
        {
            string nombre = r.ReadString();
            if (mar != null && mar.sr != null) mar.sr.sharedMaterial = MaterialDe(nombre);
        }
        if ((m & CampoEspejo.Texto) != 0)
        {
            string texto = r.ReadString();
            if (mar != null && mar.tm != null) mar.tm.text = texto;
        }
        if ((m & CampoEspejo.Fuente) != 0)
        {
            string fuente = r.ReadString();
            int tamFuente = r.ReadInt32();
            float tamCar = r.ReadSingle();
            byte ancla = r.ReadByte(), alin = r.ReadByte(), estilo = r.ReadByte();
            float inter = r.ReadSingle();
            if (mar != null && mar.tm != null)
            {
                Font f = FuenteDe(fuente);
                if (f != null) { mar.tm.font = f; if (mar.mr != null) mar.mr.sharedMaterial = f.material; }
                mar.tm.fontSize = tamFuente;
                mar.tm.characterSize = tamCar;
                mar.tm.anchor = (TextAnchor)ancla;
                mar.tm.alignment = (TextAlignment)alin;
                mar.tm.fontStyle = (FontStyle)estilo;
                mar.tm.lineSpacing = inter;
            }
        }
    }

    private Material MaterialDe(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || materialBase == null || nombre == materialBase.name) return materialBase;
        Material m;
        if (materiales.TryGetValue(nombre, out m)) return m != null ? m : materialBase;
        m = null;
        foreach (Material x in Resources.FindObjectsOfTypeAll<Material>()) if (x != null && x.name == nombre) { m = x; break; }
        materiales[nombre] = m;
        return m != null ? m : materialBase;
    }

    private Font FuenteDe(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        Font f;
        if (fuentes.TryGetValue(nombre, out f)) return f;
        f = null;
        foreach (Font x in Resources.FindObjectsOfTypeAll<Font>()) if (x != null && x.name == nombre) { f = x; break; }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>(nombre + ".ttf"); } catch { f = null; } }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { f = null; } }
        fuentes[nombre] = f;
        return f;
    }

    // ------------------------------------------------------------ movimiento suave
    private void Update()
    {
        float k = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
        foreach (Marioneta m in marionetas.Values)
        {
            if (m.go == null) continue;
            Transform t = m.go.transform;
            Vector3 p = t.position;
            if (p != m.objetivo) t.position = Vector3.Lerp(p, m.objetivo, k);
        }
    }
}
