using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La vereda frente a la entrada de Unifranz (la franja de abajo de la fachada): suelo firme con colisión a
/// todo lo ancho del edificio. Si el muñeco cae desde una repisa, aterriza aquí sin perder vida y puede volver
/// a subir. En el terremoto se agrieta y parpadea, y luego se derrumba: los trozos caen y la vereda pierde su
/// colisión hasta el final del nivel (Restablecer la deja firme otra vez al reiniciar).
/// </summary>
public class VeredaFachada : MonoBehaviour
{
    private float ppu = 50f;
    private float alto;          // alto de la franja de vereda (unidades)
    private float ancho;         // ancho de la fachada (unidades)
    private BoxCollider2D colision;
    private SpriteRenderer destello;
    private SpriteRenderer hueco;
    private SpriteRenderer sueloNormal, sueloAgrietado;
    private float escalaArte = 0.64f;   // 32 px de arte = 32 / ppu unidades: así un píxel del arte mide lo mismo que uno de la fachada
    private readonly List<SpriteRenderer> grietas = new List<SpriteRenderer>();
    private readonly List<GameObject> efectos = new List<GameObject>();
    private EdificioAlto edificio;
    private Coroutine parpadeo;

    /// <summary>La vereda sostiene al muñeco.</summary>
    public bool Firme { get; private set; }

    /// <summary>Altura de su superficie en el mundo.</summary>
    public float SuperficieY { get { return transform.position.y + alto; } }

    /// <summary>La crea bajo la fachada: 'alturaPx' de alto (parte de arriba de la vereda) y 'margenPx' a cada lado.</summary>
    public void Construir(EdificioAlto edificio, float pixelesPorUnidad, int anchoPx, int alturaPx, int margenPx)
    {
        this.edificio = edificio;
        ppu = pixelesPorUnidad;
        alto = alturaPx / ppu;
        ancho = anchoPx / ppu;
        float margen = margenPx / ppu;
        const float grosor = 0.6f;

        colision = gameObject.AddComponent<BoxCollider2D>();
        colision.size = new Vector2(ancho + margen * 2f, grosor);
        colision.offset = new Vector2(ancho * 0.5f, alto - grosor * 0.5f);

        // Destello que parpadea durante el aviso y hueco oscuro que queda al derrumbarse.
        destello = Provisional.Crear(transform, "Destello", Provisional.Cuadrado, new Color(1f, 1f, 1f, 0.5f), 4);
        destello.transform.localPosition = new Vector3(ancho * 0.5f, alto * 0.5f, 0f);
        destello.transform.localScale = new Vector3(ancho, alto, 1f);
        destello.enabled = false;

        hueco = Provisional.Crear(transform, "Hueco", Provisional.Cuadrado, new Color(0.07f, 0.07f, 0.09f, 1f), 4);
        hueco.transform.localPosition = new Vector3(ancho * 0.5f, alto * 0.5f, 0f);
        hueco.transform.localScale = new Vector3(ancho, alto + 0.04f, 1f);
        hueco.enabled = false;

        // Suelo con el arte (vereda y vereda agrietada), repetido a todo lo ancho.
        ArteNiveles arte = ArteNiveles.Actual;
        if (arte != null && arte.vereda != null)
        {
            escalaArte = 32f / Mathf.Max(1f, ppu);
            sueloNormal = CrearSuelo("SueloVereda", arte.vereda, ancho, margen);
            if (arte.veredaAgrietada != null) sueloAgrietado = CrearSuelo("SueloVeredaAgrietada", arte.veredaAgrietada, ancho, margen);
        }

        int n = 6;
        for (int i = 0; i < n; i++)
        {
            SpriteRenderer g = Provisional.Crear(transform, "Grieta" + i, Provisional.Grietas(2), Color.white, 5);
            g.transform.localPosition = new Vector3(ancho * (i + 0.5f) / n, alto * 0.5f, 0f);
            g.transform.localScale = new Vector3(ancho / n, Mathf.Max(0.4f, alto * 1.6f), 1f);
            g.enabled = false;
            grietas.Add(g);
        }
        Restablecer();
    }

    /// <summary>Franja de suelo con el sprite repetido (modo Tiled): ocupa el alto de la vereda y todo el ancho.</summary>
    private SpriteRenderer CrearSuelo(string nombre, Sprite sprite, float anchoFachada, float margen)
    {
        SpriteRenderer sr = Provisional.Crear(transform, nombre, sprite, Color.white, 3);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2((anchoFachada + margen * 2f) / escalaArte, alto / escalaArte);
        sr.transform.localScale = new Vector3(escalaArte, escalaArte, 1f);
        sr.transform.localPosition = new Vector3(anchoFachada * 0.5f, alto * 0.5f, 0f);
        return sr;
    }

    /// <summary>Vuelve a estar firme y sin grietas.</summary>
    public void Restablecer()
    {
        if (parpadeo != null) { StopCoroutine(parpadeo); parpadeo = null; }
        foreach (GameObject e in efectos) if (e != null) Destroy(e);
        efectos.Clear();
        Firme = true;
        if (colision != null) colision.enabled = true;
        if (destello != null) destello.enabled = false;
        if (hueco != null) hueco.enabled = false;
        foreach (SpriteRenderer g in grietas) g.enabled = false;
        if (sueloNormal != null) sueloNormal.enabled = true;
        if (sueloAgrietado != null) sueloAgrietado.enabled = false;
        if (edificio != null) edificio.VeredaFirme = true;
    }

    /// <summary>Muestra las grietas y hace parpadear la vereda (el aviso del terremoto).</summary>
    public void Agrietar()
    {
        // Con arte: se cambia al suelo agrietado y avanzan las grietas del terremoto; sin arte, las grietas provisionales.
        bool conArte = sueloNormal != null && sueloAgrietado != null;
        if (conArte)
        {
            sueloNormal.enabled = false;
            sueloAgrietado.enabled = true;
            LanzarGrietasTerremoto();
        }
        else foreach (SpriteRenderer g in grietas) g.enabled = true;
        if (parpadeo != null) StopCoroutine(parpadeo);
        parpadeo = StartCoroutine(Parpadear());
    }

    private IEnumerator Parpadear()
    {
        while (true)
        {
            destello.enabled = ((int)(Time.time * 10f) % 2) == 0;
            yield return null;
        }
    }

    /// <summary>Se derrumba: los trozos caen y desaparecen, y pierde la colisión hasta el final del nivel.</summary>
    public void Derrumbar()
    {
        if (parpadeo != null) { StopCoroutine(parpadeo); parpadeo = null; }
        Firme = false;
        GestorAudio.Reproducir(Snd.DerrumbeVereda);
        if (colision != null) colision.enabled = false;
        if (edificio != null) edificio.VeredaFirme = false;
        if (destello != null) destello.enabled = false;
        foreach (SpriteRenderer g in grietas) g.enabled = false;
        if (sueloNormal != null) sueloNormal.enabled = false;
        if (sueloAgrietado != null) sueloAgrietado.enabled = false;
        if (hueco != null) hueco.enabled = true;

        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] trozosArte = arte != null ? ArteNiveles.Serie(arte.trozosVereda) : null;
        for (int i = 0; i < 16; i++)
        {
            GameObject t = new GameObject("TrozoVereda");
            t.transform.position = transform.position + new Vector3(Random.Range(0.2f, ancho - 0.2f), alto, 0f);
            SpriteRenderer sr;
            if (trozosArte != null)
            {
                sr = Provisional.Crear(t.transform, "Dibujo", trozosArte[Random.Range(0, trozosArte.Length)], Color.white, 6);
                sr.transform.localScale = new Vector3(escalaArte, escalaArte, 1f);
            }
            else
            {
                float lado = Random.Range(0.14f, 0.3f);
                sr = Provisional.Crear(t.transform, "Dibujo", Provisional.Cuadrado,
                    Color.Lerp(new Color(0.55f, 0.5f, 0.45f), new Color(0.35f, 0.32f, 0.3f), Random.value), 6);
                sr.transform.localScale = new Vector3(lado, lado * Random.Range(0.6f, 1f), 1f);
            }
            t.AddComponent<TrozoCayendo>().Iniciar(new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-0.5f, 0.8f)));
        }

        // Polvo del derrumbe a lo largo de la vereda.
        Sprite[] polvo = arte != null ? ArteNiveles.Serie(arte.polvoDerrumbe) : null;
        if (polvo != null)
        {
            float paso = Mathf.Max(1.2f, polvo[0].bounds.size.x * escalaArte);
            for (float x = paso * 0.5f; x < ancho; x += paso)
                EfectoSprite.Reproducir(polvo, transform.position + new Vector3(x, alto + 0.2f, 0f), 7, escalaArte, 8f);
        }
        CamaraSeguimiento.Temblar(0.22f, 0.5f);
    }

    /// <summary>Las grietas del terremoto avanzan por la vereda (4 cuadros; el último queda).</summary>
    private void LanzarGrietasTerremoto()
    {
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] cuadros = arte != null ? ArteNiveles.Serie(arte.grietaTerremoto) : null;
        if (cuadros == null) return;

        float paso = Mathf.Max(1f, cuadros[0].bounds.size.x * escalaArte);
        for (float x = paso * 0.5f; x < ancho; x += paso)
        {
            GameObject g = new GameObject("GrietaTerremoto");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(x, alto * 0.5f, 0f);
            g.transform.localScale = new Vector3(escalaArte, escalaArte, 1f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = cuadros[0];
            sr.sortingOrder = 5;
            AnimadorSprite a = g.AddComponent<AnimadorSprite>();
            a.cuadros = cuadros;
            a.cuadrosPorSegundo = 4f;
            a.bucle = false;
            a.destruirAlTerminar = false;
            efectos.Add(g);
        }
    }

    /// <summary>Un trozo de vereda que cae con la gravedad, gira y se desvanece.</summary>
    private class TrozoCayendo : MonoBehaviour
    {
        private Vector2 velocidad;
        private float giro, edad;
        private SpriteRenderer dibujo;

        public void Iniciar(Vector2 v)
        {
            velocidad = v;
            giro = Random.Range(-360f, 360f);
            dibujo = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            edad += Time.deltaTime;
            velocidad.y -= 14f * Time.deltaTime;
            transform.position += (Vector3)(velocidad * Time.deltaTime);
            transform.Rotate(0f, 0f, giro * Time.deltaTime);
            if (dibujo != null && edad > 0.8f)
            {
                Color c = dibujo.color;
                c.a = Mathf.Clamp01(1f - (edad - 0.8f) / 0.7f);
                dibujo.color = c;
            }
            if (edad > 1.5f) Destroy(gameObject);
        }
    }
}
