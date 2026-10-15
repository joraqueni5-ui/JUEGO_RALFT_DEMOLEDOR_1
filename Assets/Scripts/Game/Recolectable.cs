using UnityEngine;

namespace DemoledorUnifranz.Game
{
    /// <summary>
    /// Ladrillo recolectable. Cuando el jugador lo toca suma puntos
    /// y desaparece de la escena.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Recolectable : MonoBehaviour
    {
        public float velocidadGiro = 90f;
        public float alturaOndulacion = 0.15f;

        private Vector3 posicionBase;
        private float desfase;

        private void Start()
        {
            posicionBase = transform.position;
            desfase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            // Pequena animacion para que el objeto se note en pantalla.
            transform.Rotate(0f, 0f, velocidadGiro * Time.deltaTime);
            float y = posicionBase.y + Mathf.Sin(Time.time * 2f + desfase) * alturaOndulacion;
            transform.position = new Vector3(posicionBase.x, y, posicionBase.z);
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (otro.GetComponent<PlayerController>() == null)
            {
                return;
            }

            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.RecolectarLadrillo();
            }

            gameObject.SetActive(false);
        }
    }
}
