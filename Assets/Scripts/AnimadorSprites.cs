using UnityEngine;

/// <summary>
/// Reproduce animaciones cuadro a cuadro sobre un SpriteRenderer.
/// No usa Animator para que las animaciones se puedan cambiar desde código
/// (caminar, saltar, golpear, reparar...).
/// </summary>
public class AnimadorSprites
{
    private readonly SpriteRenderer destino;
    private Sprite[] cuadros;
    private float segundosPorCuadro = 1f / 12f;
    private float tiempo;
    private int indice;
    private bool repetir = true;
    private bool reproduciendo;

    public AnimadorSprites(SpriteRenderer destino)
    {
        this.destino = destino;
    }

    public bool Reproduciendo { get { return reproduciendo; } }

    public Sprite[] SerieActual { get { return cuadros; } }

    /// <summary>
    /// Cambia a otra serie de cuadros. Si es la misma serie y <paramref name="reiniciar"/>
    /// es falso, la animación continúa donde iba (útil en cada frame de Update).
    /// </summary>
    public void Reproducir(Sprite[] nuevos, float cuadrosPorSegundo = 12f, bool repetir = true, bool reiniciar = true)
    {
        if (nuevos == null || nuevos.Length == 0) return;

        segundosPorCuadro = 1f / Mathf.Max(1f, cuadrosPorSegundo);
        this.repetir = repetir;

        if (reiniciar || cuadros != nuevos)
        {
            cuadros = nuevos;
            indice = 0;
            tiempo = 0f;
            reproduciendo = true;
            Mostrar();
        }
        else
        {
            reproduciendo = true;
        }
    }

    /// <summary>Muestra un cuadro fijo (por ejemplo el primero de la caminata cuando está quieto).</summary>
    public void MostrarCuadroFijo(Sprite[] serie, int cuadro = 0)
    {
        if (serie == null || serie.Length == 0) return;

        cuadros = serie;
        indice = Mathf.Clamp(cuadro, 0, serie.Length - 1);
        reproduciendo = false;
        Mostrar();
    }

    public void Detener()
    {
        reproduciendo = false;
    }

    public void Actualizar(float delta)
    {
        if (!reproduciendo || cuadros == null || cuadros.Length == 0) return;

        tiempo += delta;
        while (tiempo >= segundosPorCuadro)
        {
            tiempo -= segundosPorCuadro;
            indice++;

            if (indice >= cuadros.Length)
            {
                if (repetir)
                {
                    indice = 0;
                }
                else
                {
                    indice = cuadros.Length - 1;
                    reproduciendo = false;
                    Mostrar();
                    break;
                }
            }
            Mostrar();
        }
    }

    private void Mostrar()
    {
        if (destino == null || cuadros == null || cuadros.Length == 0) return;
        destino.sprite = cuadros[Mathf.Clamp(indice, 0, cuadros.Length - 1)];
    }
}
