using UnityEngine;

namespace RalftDemoledor.Game
{
    /// <summary>
    /// Movimiento del personaje principal (vista superior).
    /// Se usan las teclas A/D/Izquierda/Derecha y W/S/Arriba/Abajo
    /// a traves de los ejes Horizontal y Vertical del Input Manager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public float velocidad = 6f;

        private Rigidbody2D cuerpo;
        private SpriteRenderer dibujo;
        private Vector2 direccion;
        private Vector3 posicionInicial;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody2D>();
            dibujo = GetComponent<SpriteRenderer>();
            cuerpo.gravityScale = 0f;
            cuerpo.freezeRotation = true;
            posicionInicial = transform.position;
        }

        private void Update()
        {
            direccion = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            if (direccion.sqrMagnitude > 1f)
            {
                direccion.Normalize();
            }

            // Se voltea el dibujo segun la direccion del movimiento.
            if (dibujo != null && Mathf.Abs(direccion.x) > 0.01f)
            {
                dibujo.flipX = direccion.x < 0f;
            }
        }

        private void FixedUpdate()
        {
            cuerpo.linearVelocity = direccion * velocidad;
        }

        /// <summary>Devuelve al personaje al punto de partida del nivel.</summary>
        public void ReiniciarPosicion()
        {
            transform.position = posicionInicial;

            if (cuerpo != null)
            {
                cuerpo.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>Define el punto de partida del personaje.</summary>
        public void DefinirPuntoDeInicio(Vector3 punto)
        {
            posicionInicial = punto;
            transform.position = punto;
        }
    }
}
