using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Vida del fondo del menú: dos reflectores que barren el cielo, motas de polvo que suben y la luz roja de la grúa
    /// que parpadea. La agrega TemaSerio al cambiar el muro por el fondo nocturno.
    /// </summary>
    public class FondoMenuAnimado : MonoBehaviour
    {
        [HideInInspector] public Transform despuesDe;
        public int motas = 26;

        private RectTransform[] haces;
        private float[] fasesHaz;
        private RectTransform[] particulas;
        private Image[] imgParticulas;
        private Vector2[] velocidades;
        private Image luzGrua;
        private RectTransform area;

        private void Start()
        {
            area = (RectTransform)transform;
            int indice = despuesDe != null ? despuesDe.GetSiblingIndex() + 1 : 1;

            // Reflectores (detrás de los personajes, encima del fondo).
            Sprite haz = ArteExtra.Uno("Tema/haz_luz");
            haces = new RectTransform[2];
            fasesHaz = new float[] { 0f, 2.1f };
            Vector2[] bases = { new Vector2(-0.18f, 0f), new Vector2(0.98f, 0f) };
            for (int i = 0; i < haces.Length; i++)
            {
                Image img = Crear("Reflector" + i, haz, new Color(1f, 0.95f, 0.8f, 0.55f));
                RectTransform r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(bases[i].x < 0 ? 0.32f : 0.9f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.sizeDelta = new Vector2(260f, 1300f);
                r.anchoredPosition = new Vector2(0f, 60f);
                r.SetSiblingIndex(indice + i);
                haces[i] = r;
            }

            // Luz roja de la grúa (punta de la torre en el arte: x 198, y 23 de 480 x 270).
            // Va dentro del propio fondo para quedar en su lugar aunque la pantalla no sea 16:9.
            luzGrua = Crear("LuzGrua", null, new Color(1f, 0.2f, 0.15f, 1f));
            RectTransform rl = luzGrua.rectTransform;
            if (despuesDe != null) rl.SetParent(despuesDe, false);
            rl.anchorMin = rl.anchorMax = new Vector2(199f / 480f, 1f - 23.5f / 270f);
            rl.sizeDelta = new Vector2(10f, 6f);

            // Motas de polvo.
            Sprite mota = ArteExtra.Uno("Tema/mota");
            particulas = new RectTransform[motas];
            imgParticulas = new Image[motas];
            velocidades = new Vector2[motas];
            for (int i = 0; i < motas; i++)
            {
                Image img = Crear("Mota" + i, mota, new Color(1f, 0.95f, 0.85f, 0f));
                float t = Random.Range(8f, 16f);
                img.rectTransform.sizeDelta = new Vector2(t, t);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                particulas[i] = img.rectTransform;
                imgParticulas[i] = img;
                velocidades[i] = new Vector2(Random.Range(-12f, 12f), Random.Range(14f, 40f));
                Reubicar(i, true);
            }
        }

        private Image Crear(string nombre, Sprite sprite, Color color)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(transform, false);
            Image img = g.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void Reubicar(int i, bool enCualquierLado)
        {
            Rect r = area.rect;
            float y = enCualquierLado ? Random.Range(r.yMin, r.yMax) : r.yMin - 10f;
            particulas[i].anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), y);
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;
            if (haces != null)
                for (int i = 0; i < haces.Length; i++)
                    haces[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.35f + fasesHaz[i]) * 28f + (i == 0 ? -12f : 14f));

            if (luzGrua != null) luzGrua.enabled = (t % 1.6f) < 0.8f;

            if (particulas == null) return;
            Rect r = area.rect;
            for (int i = 0; i < particulas.Length; i++)
            {
                Vector2 v = velocidades[i] + new Vector2(Mathf.Sin(t * 0.8f + i) * 10f, 0f);
                particulas[i].anchoredPosition += v * dt;
                if (particulas[i].anchoredPosition.y > r.yMax + 10f) Reubicar(i, false);
                imgParticulas[i].color = new Color(1f, 0.95f, 0.85f, 0.18f + 0.14f * Mathf.Sin(t * 1.4f + i * 1.7f));
            }
        }
    }
}
