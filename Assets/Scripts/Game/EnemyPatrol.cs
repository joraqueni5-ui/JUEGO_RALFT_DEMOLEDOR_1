using UnityEngine;

namespace RalftDemoledor.Game
{
    /// <summary>
    /// Enemigo del nivel. Se desplaza de un punto a otro (patrulla)
    /// y quita una vida cuando toca al jugador.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyPatrol : MonoBehaviour
    {
        public Vector2 puntoA = new Vector2(-3f, 2f);
        public Vector2 puntoB = new Vector2(3f, 2f);
        public float velocidad = 2.5f;
        public float tiempoEntreDanios = 1.5f;

        private Rigidbody2D cuerpo;
        private Vector2 destino;
        private float proximoDanio;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody2D>();
            destino = puntoB;
        }

        private void FixedUpdate()
        {
            Vector2 actual = cuerpo.position;

            if (Vector2.Distance(actual, destino) < 0.15f)
            {
                destino = (destino == puntoA) ? puntoB : puntoA;
            }

            Vector2 siguiente = Vector2.MoveTowards(actual, destino, velocidad * Time.fixedDeltaTime);
            cuerpo.MovePosition(siguiente);

            // Orienta el sprite hacia donde camina.
            if (Mathf.Abs(siguiente.x - actual.x) > 0.001f)
            {
                Vector3 escala = transform.localScale;
                escala.x = Mathf.Abs(escala.x) * Mathf.Sign(siguiente.x - actual.x);
                transform.localScale = escala;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            PlayerController jugador = otro.GetComponent<PlayerController>();

            if (jugador == null || Time.time < proximoDanio)
            {
                return;
            }

            proximoDanio = Time.time + tiempoEntreDanios;
            jugador.ReiniciarPosicion();

            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.RecibirDanio();
            }
        }
    }
}
