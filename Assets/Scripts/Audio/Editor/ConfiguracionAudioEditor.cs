using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importación del audio y armado de la biblioteca de sonidos.
/// - efectos/: Decompress On Load, ADPCM, Preload Audio Data y Force To Mono.
/// - musica/: Streaming, Vorbis, calidad 70 %.
/// Los mismos ajustes valen para Windows y Android (no se dejan reemplazos por plataforma).
/// Los archivos originales no se tocan.
/// </summary>
public class ImportadorAudio : AssetPostprocessor
{
    public const string Raiz = "Assets/Audio";

    public override uint GetVersion() { return 1; }

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Raiz + "/", StringComparison.Ordinal)) return;
        Aplicar((AudioImporter)assetImporter, assetPath);
    }

    public static bool Aplicar(AudioImporter imp, string ruta)
    {
        bool efecto = ruta.Contains("/Efectos/");
        AudioImporterSampleSettings s = imp.defaultSampleSettings;
        s.loadType = efecto ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
        s.compressionFormat = efecto ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis;
        if (!efecto) s.quality = 0.7f;
        s.preloadAudioData = efecto;
        imp.defaultSampleSettings = s;
        imp.forceToMono = efecto;
        imp.loadInBackground = false;
        imp.ClearSampleSettingOverride("Standalone");
        imp.ClearSampleSettingOverride("Android");
        return true;
    }

}
