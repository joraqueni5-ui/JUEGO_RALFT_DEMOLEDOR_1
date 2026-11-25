using UnityEngine;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Conecta los extras sin tener que editar las escenas: al cargar cada escena
///   - aplica el tema uniforme a los menús (TemaSerio),
///   - en cada piso cenital agrega GestorMiniVillanos, GestorPotenciadoresExtra e IntroNivel (escena de llegada),
///   - en los pisos y en el nivel final crea el ambiente visual (AmbienteNivel).
/// El sonido de ambiente lo pone GestorAudio según la escena.
/// </summary>
public static class InstaladorExtras
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);   // la escena con la que arrancó el juego
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        TemaSerio.RestilizarEscena();
        Progreso.EmpezarNivel();

        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.gameObject.scene == escena && RedJuego.EsInvitadoEnPartida)
        {
            // Invitado del multijugador: el anfitrión simula; aquí solo el espejo, la decoración y el ambiente.
            MultijugadorNivel.PrepararInvitado(piso);
            if (Object.FindAnyObjectByType<DetallesEscenario>() == null) piso.gameObject.AddComponent<DetallesEscenario>();
        }
        else if (piso != null && piso.gameObject.scene == escena)
        {
            // Anfitrión del multijugador: segundo estudiante (remoto) y segundo demoledor.
            if (RedJuego.EsAnfitrionEnPartida) MultijugadorNivel.PrepararAnfitrion(piso);

            // Una cuadrilla de mini villanos por demoledor.
            bool primera = true;
            foreach (DemoledorCenital d in Object.FindObjectsByType<DemoledorCenital>(FindObjectsInactive.Exclude))
            {
                GestorMiniVillanos gm = primera ? piso.GetComponent<GestorMiniVillanos>() : null;
                if (gm == null) gm = piso.gameObject.AddComponent<GestorMiniVillanos>();
                gm.demoledorAsignado = d;
                primera = false;
                // Unos segundos de calma (gente caminando) antes de que llegue el demoledor.
                d.esperaInicial = Mathf.Max(d.esperaInicial, 5.5f);
            }
            if (piso.GetComponent<GestorMiniVillanos>() == null) piso.gameObject.AddComponent<GestorMiniVillanos>();
            if (piso.GetComponent<GestorPotenciadoresExtra>() == null) piso.gameObject.AddComponent<GestorPotenciadoresExtra>();
            if (piso.GetComponent<IntroNivel>() == null) piso.gameObject.AddComponent<IntroNivel>();
            foreach (JugadorCenital jug in Object.FindObjectsByType<JugadorCenital>(FindObjectsInactive.Exclude))
            {
                if (jug.GetComponent<ComentariosJugador>() == null) jug.gameObject.AddComponent<ComentariosJugador>();
                if (jug.GetComponent<IraEstudiante>() == null) jug.gameObject.AddComponent<IraEstudiante>();
            }
            if (Object.FindAnyObjectByType<DetallesEscenario>() == null) piso.gameObject.AddComponent<DetallesEscenario>();
        }

        // En los dos equipos (cada uno lo arma igual): luces, cosas colgadas en las paredes y humo/chispas del daño.
        if (piso != null && piso.gameObject.scene == escena)
        {
            if (piso.GetComponent<IluminacionNivel>() == null) piso.gameObject.AddComponent<IluminacionNivel>();
            if (piso.GetComponent<DecoracionParedes>() == null) piso.gameObject.AddComponent<DecoracionParedes>();
            if (piso.GetComponent<DanoVisible>() == null) piso.gameObject.AddComponent<DanoVisible>();
            if (piso.GetComponent<AnimacionesAmbiente>() == null) piso.gameObject.AddComponent<AnimacionesAmbiente>();
            // Vida del escenario: vistas a La Paz, afiches de la U, objetos vivos, daño visible y reparar con gracia.
            if (piso.GetComponent<VidaEscenario>() == null) piso.gameObject.AddComponent<VidaEscenario>();
            // Modo supervivencia: oleadas de villanos (en los dos equipos: el anfitrión las maneja, el invitado ve el marcador).
            if (GameSession.Supervivencia && piso.GetComponent<ModoSupervivencia>() == null) piso.gameObject.AddComponent<ModoSupervivencia>();
            // El peligro propio del piso (charcos, cables, apagones, goteras o viento).
            if (piso.GetComponent<PeligrosPiso>() == null) piso.gameObject.AddComponent<PeligrosPiso>();
            // El accesorio elegido en PREMIOS, en tu personaje.
            foreach (JugadorCenital jug in Object.FindObjectsByType<JugadorCenital>(FindObjectsInactive.Exclude))
                if (!jug.controlRemoto) { AccesorioJugador.Poner(jug.gameObject); break; }
        }

        // Nivel final: nubes y pájaros en el cielo, el jefe final con sus 3 fases y el accesorio en el muñeco.
        if (escena.name == Escenas.NivelFinal && Object.FindAnyObjectByType<AmbienteFachada>() == null)
        {
            GameObject cielo = new GameObject("AmbienteFachada");
            SceneManager.MoveGameObjectToScene(cielo, escena);
            cielo.AddComponent<AmbienteFachada>();
        }
        if (escena.name == Escenas.NivelFinal && Object.FindAnyObjectByType<JefeFinal>() == null)
        {
            GameObject jefe = new GameObject("JefeFinal");
            SceneManager.MoveGameObjectToScene(jefe, escena);
            jefe.AddComponent<JefeFinal>();
            jefe.AddComponent<FuriaFachada>();     // la furia del estudiante también en la fachada
            jefe.AddComponent<IndicadorSubir>();   // si te caes: flecha "¡vuelve a subir!"
            jefe.AddComponent<PlataformasFachada>();   // sin tubos: andamios entre repisas y escalones en la planta baja
            jefe.AddComponent<IntroFinalJefe>();   // cinemática de entrada: la cámara sube, el demoledor ruge y la pantalla VS
            Muneco m = Object.FindAnyObjectByType<Muneco>();
            if (m != null) AccesorioJugador.Poner(m.gameObject);
        }

        bool esNivel = (piso != null && piso.gameObject.scene == escena) || escena.name == Escenas.NivelFinal;
        if (esNivel && Object.FindAnyObjectByType<AmbienteNivel>() == null)
        {
            GameObject g = new GameObject("[AmbienteNivel]");
            SceneManager.MoveGameObjectToScene(g, escena);
            g.AddComponent<AmbienteNivel>();
        }
    }
}
