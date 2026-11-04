using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Transición entre escenas: una pared de ladrillos cae por franjas desde arriba
    /// y tapa la pantalla (Cerrar); después de cargar la escena, las franjas se caen
    /// hacia abajo y la destapan (Abrir). Usa tiempo sin escala.
    /// La crea ServiciosMenus y vive en DontDestroyOnLoad.
    /// </summary>
    public class TransicionLadrillos : MonoBehaviour
    {
        private const float AltoFranja = 128f;      // 4 filas de ladrillos (el muro pixel ×4)
        private const float Gravedad = 5200f;
        private const float RetrasoEntreFranjas = 0.045f;

        private RectTransform zona;
        private Image bloqueo;
        private readonly List<RectTransform> franjas = new List<RectTransform>();
        private Sprite muro;

        public bool Tapada { get; private set; }

        public static TransicionLadrillos Crear(Transform padre)
        {
            Canvas canvas = FabricaUIArcade.CrearCanvas("TransicionLadrillos", 1000);
            canvas.transform.SetParent(padre, false);
            TransicionLadrillos t = canvas.gameObject.AddComponent<TransicionLadrillos>();
            t.zona = (RectTransform)canvas.transform;

            // Bloquea los clics mientras dura la transición.
            t.bloqueo = FabricaUIArcade.CrearImagen(canvas.transform, "Bloqueo", null, new Color(0f, 0f, 0f, 0f), true);
            FabricaUIArcade.Estirar(t.bloqueo.rectTransform);
            t.bloqueo.gameObject.SetActive(false);
            return t;
        }

        private Sprite Muro()
        {
            if (muro != null) return muro;
            KitSpritesArcade kit = KitSpritesArcade.Actual();
            if (kit != null && kit.muro != null) muro = kit.muro;
            else
            {
                Texture2D tex = GeneradorPixelArt.Muro();
                muro = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            return muro;
        }

        private void PrepararFranjas()
        {
            float alto = zona.rect.height;
            int cantidad = Mathf.CeilToInt(alto / AltoFranja) + 1;
            Sprite sprite = Muro();

            while (franjas.Count < cantidad)
            {
                Image img = FabricaUIArcade.CrearImagen(zona, "Franja" + franjas.Count, sprite, Color.white);
                img.type = Image.Type.Tiled;
                img.pixelsPerUnitMultiplier = KitSpritesArcade.MultiplicadorPixel;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, AltoFranja);
                // Sombra en el canto inferior de cada franja para que se note el golpe.
                Image sombra = FabricaUIArcade.CrearImagen(rt, "Sombra", null, new Color(0f, 0f, 0f, 0.35f));
                RectTransform rs = sombra.rectTransform;
                rs.anchorMin = new Vector2(0f, 0f);
                rs.anchorMax = new Vector2(1f, 0f);
                rs.pivot = new Vector2(0.5f, 0f);
                rs.sizeDelta = new Vector2(0f, 8f);
                franjas.Add(rt);
            }
            for (int i = 0; i < franjas.Count; i++) franjas[i].gameObject.SetActive(i < cantidad);
        }

        /// <summary>La pared cae y tapa la pantalla (de abajo hacia arriba).</summary>
        public IEnumerator Cerrar()
        {
            bloqueo.gameObject.SetActive(true);
            PrepararFranjas();
            float alto = zona.rect.height;
            int n = ActivasCount();

            float[] destino = new float[n], inicio = new float[n], vel = new float[n];
            bool[] llego = new bool[n];
            for (int i = 0; i < n; i++)
            {
                destino[i] = i * AltoFranja;
                inicio[i] = alto + 40f + i * 30f;
                franjas[i].anchoredPosition = new Vector2(0f, inicio[i]);
            }

            float t = 0f;
            int listas = 0;
            while (listas < n)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                t += dt;
                for (int i = 0; i < n; i++)
                {
                    if (llego[i] || t < i * RetrasoEntreFranjas) continue;
                    vel[i] += Gravedad * dt;
                    float y = franjas[i].anchoredPosition.y - vel[i] * dt;
                    if (y <= destino[i])
                    {
                        y = destino[i];
                        llego[i] = true;
                        listas++;
                        if (i % 2 == 0 && ServiciosMenus.Existe) ServiciosMenus.Instancia.Golpe();
                    }
                    franjas[i].anchoredPosition = new Vector2(0f, y);
                }
                yield return null;
            }
            Tapada = true;
            yield return Temblor(0.12f, 6f);
        }

        /// <summary>Las franjas se caen hacia abajo y destapan la escena nueva.</summary>
        public IEnumerator Abrir()
        {
            int n = ActivasCount();
            float[] vel = new float[n];
            float t = 0f;
            bool alguna = true;
            while (alguna)
            {
                alguna = false;
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                t += dt;
                for (int i = 0; i < n; i++)
                {
                    // Primero se van las de abajo, como una pared que se desmorona.
                    if (t < i * RetrasoEntreFranjas * 0.8f) { alguna = true; continue; }
                    RectTransform f = franjas[i];
                    if (f.anchoredPosition.y < -AltoFranja - 20f) continue;
                    vel[i] += Gravedad * dt;
                    f.anchoredPosition = new Vector2(0f, f.anchoredPosition.y - vel[i] * dt);
                    alguna = true;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++) franjas[i].gameObject.SetActive(false);
            Tapada = false;
            bloqueo.gameObject.SetActive(false);
        }

        private IEnumerator Temblor(float duracion, float fuerza)
        {
            float t = 0f;
            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / duracion;
                float dx = Mathf.Round(Random.Range(-1f, 1f)) * fuerza * k;
                for (int i = 0; i < franjas.Count; i++)
                    if (franjas[i].gameObject.activeSelf)
                        franjas[i].anchoredPosition = new Vector2(dx, franjas[i].anchoredPosition.y);
                yield return null;
            }
            for (int i = 0; i < franjas.Count; i++)
                franjas[i].anchoredPosition = new Vector2(0f, franjas[i].anchoredPosition.y);
        }

        private int ActivasCount()
        {
            int n = 0;
            for (int i = 0; i < franjas.Count; i++) if (franjas[i].gameObject.activeSelf) n++;
            return n;
        }
    }
}
