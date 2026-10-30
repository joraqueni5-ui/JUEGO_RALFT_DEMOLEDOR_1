using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ladrillos decorativos: pueden caer de vez en cuando desde arriba (lluvia) y
    /// salir disparados desde un punto (Estallido), por ejemplo cuando el demoledor golpea.
    /// Es solo decoración: no recibe clics.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EscombrosDecorativos : MonoBehaviour
    {
        public Sprite sprite;
        [Tooltip("Si está activo, caen ladrillos desde arriba de vez en cuando.")]
        public bool lluviaAutomatica = true;
        [Tooltip("Cantidad máxima de ladrillos a la vez.")]
        public int maximo = 8;
        public Vector2 intervalo = new Vector2(0.5f, 1.3f);
        public Vector2 tamano = new Vector2(48f, 24f);
        [Range(0f, 1f)] public float opacidad = 0.55f;

        private class Pieza
        {
            public RectTransform rt;
            public Image imagen;
            public Vector2 velocidad;
            public float giro;
            public bool activa;
        }

        private readonly List<Pieza> piezas = new List<Pieza>();
        private RectTransform area;
        private float siguiente;

        private void Awake()
        {
            area = (RectTransform)transform;
        }

        private void OnEnable()
        {
            siguiente = Time.unscaledTime + 0.4f;
        }

        private void Update()
        {
            if (sprite == null) return;
            float dt = Time.unscaledDeltaTime;

            if (lluviaAutomatica && Time.unscaledTime >= siguiente)
            {
                siguiente = Time.unscaledTime + Random.Range(intervalo.x, intervalo.y);
                Lanzar();
            }

            float limite = -area.rect.height * 0.5f - 80f;
            for (int i = 0; i < piezas.Count; i++)
            {
                Pieza p = piezas[i];
                if (!p.activa) continue;

                p.velocidad.y -= 900f * dt;
                p.rt.anchoredPosition += p.velocidad * dt;
                p.rt.Rotate(0f, 0f, p.giro * dt);

                if (p.rt.anchoredPosition.y < limite)
                {
                    p.activa = false;
                    p.rt.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Lanza varios ladrillos hacia arriba desde un punto (coordenadas locales).</summary>
        public void Estallido(Vector2 origen, int cantidad)
        {
            if (sprite == null) return;
            for (int i = 0; i < cantidad; i++)
            {
                Pieza p = Obtener();
                if (p == null) return;
                Preparar(p, Random.Range(0.45f, 0.9f));
                p.rt.anchoredPosition = origen + new Vector2(Random.Range(-90f, 90f), Random.Range(-10f, 20f));
                p.velocidad = new Vector2(Random.Range(-380f, 380f), Random.Range(260f, 620f));
            }
        }

        private Pieza Obtener()
        {
            for (int i = 0; i < piezas.Count; i++)
                if (!piezas[i].activa) return piezas[i];

            if (piezas.Count >= maximo) return null;
            GameObject objeto = new GameObject("Ladrillo", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(transform, false);
            Pieza nueva = new Pieza { rt = (RectTransform)objeto.transform, imagen = objeto.GetComponent<Image>() };
            nueva.imagen.sprite = sprite;
            nueva.imagen.raycastTarget = false;
            piezas.Add(nueva);
            return nueva;
        }

        private void Preparar(Pieza p, float escala)
        {
            p.rt.sizeDelta = tamano * escala;
            p.rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            p.giro = Random.Range(-360f, 360f);
            p.imagen.color = new Color(1f, 1f, 1f, opacidad * Random.Range(0.75f, 1f));
            p.activa = true;
            p.rt.gameObject.SetActive(true);
        }

        private void Lanzar()
        {
            Pieza p = Obtener();
            if (p == null) return;
            Preparar(p, Random.Range(0.6f, 1.25f));
            float mitadAncho = area.rect.width * 0.5f;
            p.rt.anchoredPosition = new Vector2(Random.Range(-mitadAncho, mitadAncho), area.rect.height * 0.5f + 60f);
            p.velocidad = new Vector2(Random.Range(-60f, 60f), Random.Range(-80f, 0f));
        }
    }
}
