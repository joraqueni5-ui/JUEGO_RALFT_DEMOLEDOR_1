using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Fondo del menú principal: reproduce en bucle un video corto de una partida real
    /// ("modo demo"). Si no hay video asignado o falla (por ejemplo en algún celular),
    /// se queda la escena fija de respaldo (muro con el demoledor). El juego nunca se carga.
    /// </summary>
    public class FondoVideoMenu : MonoBehaviour
    {
        [Tooltip("Video de la partida (Assets/Sprites/Menus/Video). Vacío = se usa el respaldo.")]
        public VideoClip clip;
        [Tooltip("Imagen donde se dibuja el video.")]
        public RawImage pantalla;
        [Tooltip("Escena fija que se muestra mientras no haya video.")]
        public GameObject respaldo;
        [Tooltip("Etiqueta \"DEMO EN VIVO\" (solo se muestra con video).")]
        public GameObject etiquetaDemo;
        [Tooltip("Color/ambiente que se aplica encima del video (blanco = sin cambio).")]
        public Color tinte = Color.white;

        private VideoPlayer reproductor;
        private RenderTexture textura;

        private void Start()
        {
            MostrarVideo(false);
            if (clip == null || pantalla == null) return;

            textura = new RenderTexture((int)Mathf.Max(16, clip.width), (int)Mathf.Max(16, clip.height), 0);
            textura.name = "VideoMenu";

            reproductor = gameObject.AddComponent<VideoPlayer>();
            reproductor.playOnAwake = false;
            reproductor.source = VideoSource.VideoClip;
            reproductor.clip = clip;
            reproductor.isLooping = true;
            reproductor.renderMode = VideoRenderMode.RenderTexture;
            reproductor.targetTexture = textura;
            reproductor.audioOutputMode = VideoAudioOutputMode.None;
            reproductor.skipOnDrop = true;
            reproductor.prepareCompleted += AlPrepararse;
            reproductor.errorReceived += AlFallar;
            reproductor.Prepare();
        }

        private void AlPrepararse(VideoPlayer vp)
        {
            pantalla.texture = textura;
            pantalla.color = tinte;
            vp.Play();
            MostrarVideo(true);
        }

        private void AlFallar(VideoPlayer vp, string mensaje)
        {
            Debug.LogWarning("[Menus] No se pudo reproducir el video del menú: " + mensaje + ". Se usa el fondo fijo.");
            MostrarVideo(false);
        }

        private void MostrarVideo(bool hayVideo)
        {
            if (pantalla != null) pantalla.gameObject.SetActive(hayVideo);
            if (respaldo != null) respaldo.SetActive(!hayVideo);
            if (etiquetaDemo != null) etiquetaDemo.SetActive(hayVideo);
        }

        private void OnDestroy()
        {
            if (reproductor != null)
            {
                reproductor.prepareCompleted -= AlPrepararse;
                reproductor.errorReceived -= AlFallar;
            }
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
            }
        }
    }
}
