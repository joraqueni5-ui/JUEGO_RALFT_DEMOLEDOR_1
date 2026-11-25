#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba automática de los extras (solo en el editor). Con el juego en marcha en un piso, pulsa F10:
/// crea un mini villano de cada tipo, aplica los cuatro potenciadores, congela, prueba el empujón y la música de amenaza,
/// y escribe el resultado en la consola con el prefijo [PruebaExtras]. Al final lo deja todo como estaba.
/// </summary>
public class PruebaExtras : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        GameObject g = new GameObject("[PruebaExtras]");
        DontDestroyOnLoad(g);
        g.hideFlags = HideFlags.HideInHierarchy;
        g.AddComponent<PruebaExtras>();
    }

    private bool corriendo;

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || corriendo || !k[Key.F10].wasPressedThisFrame) return;
        StartCoroutine(Probar());
    }

    private static void Log(string t) { Debug.Log("[PruebaExtras] " + t); }

    private IEnumerator Probar()
    {
        corriendo = true;
        int fallos = 0;
        Log("=== INICIO en escena " + SceneManager.GetActiveScene().name + " ===");

        PisoCenital piso = PisoCenital.Actual;
        JugadorCenital jugador = FindAnyObjectByType<JugadorCenital>();
        DemoledorCenital demoledor = FindAnyObjectByType<DemoledorCenital>();
        Log("Piso: " + (piso != null) + "  Jugador: " + (jugador != null) + "  Demoledor: " + (demoledor != null) +
            "  GestorMini: " + (FindAnyObjectByType<GestorMiniVillanos>() != null) +
            "  GestorPotenciadores: " + (FindAnyObjectByType<GestorPotenciadoresExtra>() != null) +
            "  Ambiente: " + (FindAnyObjectByType<AmbienteNivel>() != null));

        // Arte y sonidos.
        string[] sprites = { "Mini/aprendiz_naranja/frente_0", "Mini/termita_cobre/camina_0", "Mini/topadora_amarilla/avanza_0",
                             "Poderes/poder_turbo", "Poderes/poder_martillo", "Poderes/poder_reloj", "Poderes/poder_casco",
                             "Efectos/onda_empujon_0", "Efectos/humo_poof_0", "Efectos/alerta_0", "Tema/boton_serio", "Tema/fondo_menu_noche" };
        foreach (string s in sprites) if (ArteExtra.Uno(s) == null) { Log("FALTA sprite " + s); fallos++; }
        string[] sonidos = { SndExtra.RugidoDemoledor, SndExtra.EmpujonDemoledor, SndExtra.CapaAmenaza, SndExtra.AmbienteUniversidad, SndExtra.MiniLlamada };
        foreach (string s in sonidos) if (Resources.Load<AudioClip>(SndExtra.Carpeta + s) == null) { Log("FALTA sonido " + s); fallos++; }
        Log("Arte y sonidos revisados.");

        if (piso == null || jugador == null || piso.Mapa == null)
        {
            Log("No es un piso cenital: solo se revisó el arte. Fallos: " + fallos);
            corriendo = false;
            yield break;
        }

        // Mini villanos.
        Vector2 cerca = jugador.Centro;
        foreach (TipoMini tipo in new[] { TipoMini.Aprendiz, TipoMini.Termita, TipoMini.Topadora })
        {
            Vector2Int celda;
            Vector2 pos = piso.Mapa.CeldaTransitableCercana(cerca + new Vector2(3f, 0f), out celda) ? piso.Mapa.CentroDe(celda) : cerca + Vector2.right * 3f;
            try { MiniVillano.Crear(tipo, false, pos); Log("Mini creado: " + tipo); }
            catch (Exception e) { Log("ERROR creando " + tipo + ": " + e); fallos++; }
        }
        yield return new WaitForSeconds(2f);
        Log("Minis activos tras 2 s: " + MiniVillano.Activos.Count);

        // Potenciadores.
        foreach (PoderPiso p in GestorPotenciadoresExtra.Todos)
        {
            try { string m = jugador.AplicarPoder(p); Log("Poder " + p.nombre + " aplicado (" + (m ?? "ok") + "). Sprite: " + (p.sprite != null)); }
            catch (Exception e) { Log("ERROR poder " + p.nombre + ": " + e); fallos++; }
        }
        Log("HUD poderes: '" + jugador.TextoPoderes() + "'  Turbo=" + jugador.Turbo + " Martillo=" + jugador.MartilloRapido + " Casco=" + jugador.TieneCasco);
        yield return new WaitForSeconds(1f);

        // Martillazo a los minis cercanos.
        try { int n = GestorMiniVillanos.GolpearEnRadio(jugador.Centro, 50f); Log("Martillazo de prueba golpeó a " + n + " minis."); }
        catch (Exception e) { Log("ERROR martillazo: " + e); fallos++; }
        yield return new WaitForSeconds(0.5f);

        // Empujón (el casco absorbe el daño, así no se pierde vida en la prueba).
        if (demoledor != null)
        {
            try
            {
                bool perdio = jugador.RecibirEmpujon(demoledor.transform.position, 2.8f, 0.3f, true);
                Log("Empujón aplicado. Perdió vida: " + perdio + "  Casco tras el golpe: " + jugador.TieneCasco);
            }
            catch (Exception e) { Log("ERROR empujón: " + e); fallos++; }
            Log("Demoledor en piso: " + demoledor.EnPiso + "  Congelado: " + demoledor.Congelado + "  Furia: " + demoledor.EnFuria);
        }

        // Música de amenaza.
        for (int i = 0; i < 30; i++) { GestorAudio.Amenaza(1f); yield return null; }
        Log("Capa de amenaza pedida al máximo durante 30 cuadros (debería oírse el latido).");

        yield return new WaitForSeconds(1f);
        GestorMiniVillanos.EliminarTodos(false);
        Log("=== FIN. Fallos: " + fallos + " ===");
        corriendo = false;
    }
}
#endif
