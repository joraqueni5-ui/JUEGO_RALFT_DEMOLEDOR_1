using System;
using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Animación cuadro a cuadro sobre una Image de UI (por ejemplo, el demoledor
    /// golpeando en el menú). Es la versión para interfaz de AnimadorSprites, que
    /// trabaja con SpriteRenderer. Usa tiempo sin escala.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AnimadorImagenUI : MonoBehaviour
    {
        [Tooltip("Cuadros de la animación, en orden.")]
        public Sprite[] cuadros;
        [Tooltip("Cuadros por segundo.")]
        public float cuadrosPorSegundo = 10f;
        [Tooltip("Pausa (segundos) al terminar cada vuelta, antes de repetir.")]
        public float pausaEntreVueltas = 0.6f;
        [Tooltip("Cuadro en el que ocurre el golpe (-1 = ninguno).")]
        public int cuadroImpacto = -1;

        /// <summary>Se dispara al mostrarse el cuadro de impacto.</summary>
        public event Action Impacto;

        private Image imagen;
        private int indice;
        private float reloj;
        private float espera;

        private void Awake()
        {
            imagen = GetComponent<Image>();
        }

        private void OnEnable()
        {
            indice = 0;
            reloj = 0f;
            espera = 0f;
            Mostrar();
        }

        private void Update()
        {
            if (cuadros == null || cuadros.Length == 0) return;

            if (espera > 0f)
            {
                espera -= Time.unscaledDeltaTime;
                return;
            }

            reloj += Time.unscaledDeltaTime;
            float duracion = 1f / Mathf.Max(1f, cuadrosPorSegundo);
            while (reloj >= duracion)
            {
                reloj -= duracion;
                indice++;
                if (indice >= cuadros.Length)
                {
                    indice = 0;
                    espera = pausaEntreVueltas;
                    Mostrar();
                    return;
                }
                Mostrar();
                if (indice == cuadroImpacto && Impacto != null) Impacto();
            }
        }

        private void Mostrar()
        {
            if (imagen != null && cuadros != null && cuadros.Length > 0 && cuadros[indice] != null)
                imagen.sprite = cuadros[indice];
        }
    }
}
