using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Construye el ejecutable de PC y el APK de Android en EjecutableyAPK_H4/Builds (reemplaza los anteriores).
///   - Menú: REPARALOFRANZ → Construir PC y APK (o solo uno).
///   - Automático: si existe EjecutableyAPK_H4/Builds/CONSTRUIR.txt, al abrir/compilar Unity lo borra y construye.
///     (Con "solo_pc" o "solo_apk" dentro del archivo, construye solo ese.)
/// El resultado queda en EjecutableyAPK_H4/Builds/ultimo_build.txt.
/// </summary>
[InitializeOnLoad]
public static class ConstructorBuilds
{
    private const string Carpeta = "EjecutableyAPK_H4/Builds";
    private const string RutaPc = Carpeta + "/PC/Demoledor Unifranz.exe";
    private const string RutaApk = Carpeta + "/Android/DemoledorUnifranz.apk";
    private const string IdAndroid = "com.reparalofranz.juego";
    private const string Version = "2.0";
    private const int CodigoVersion = 2;
    private const string RutaIcono = "Assets/Icono/icono_app.png";
    private const string RutaLogo = "Assets/Icono/logo_inicio.png";
    private static string Pedido { get { return Path.Combine(Carpeta, "CONSTRUIR.txt"); } }
    private static string Informe { get { return Path.Combine(Carpeta, "ultimo_build.txt"); } }

    static ConstructorBuilds()
    {
        EditorApplication.delayCall += Revisar;
    }

    private static void Revisar()
    {
        if (!File.Exists(Pedido)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Revisar;
            return;
        }
        string contenido = "";
        try { contenido = File.ReadAllText(Pedido); File.Delete(Pedido); }
        catch (Exception e) { Debug.LogWarning("[Builds] " + e.Message); return; }
        Construir(!contenido.Contains("solo_apk"), !contenido.Contains("solo_pc"));
    }

    [MenuItem("REPARALOFRANZ/Construir PC y APK")]
    public static void ConstruirAmbos() { Construir(true, true); }

    [MenuItem("REPARALOFRANZ/Construir solo PC")]
    public static void ConstruirPc() { Construir(true, false); }

    [MenuItem("REPARALOFRANZ/Construir solo APK")]
    public static void ConstruirApk() { Construir(false, true); }

    /// <summary>Versión, icono de la aplicación y logo de la pantalla de inicio (en vez del de Unity).</summary>
    private static void AplicarIdentidad(StringBuilder log)
    {
        try
        {
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, CodigoVersion);
            Texture2D icono = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaIcono);
            if (icono != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);
            Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaLogo);
            if (logo != null)
            {
                PlayerSettings.SplashScreen.show = true;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
                PlayerSettings.SplashScreen.backgroundColor = new Color(0.04f, 0.05f, 0.11f);
                PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
                PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            }
            AssetDatabase.SaveAssets();
            log.AppendLine("Versión " + Version + (icono != null ? ", icono propio" : ", SIN icono") + (logo != null ? ", logo de inicio" : ", SIN logo"));
        }
        catch (Exception e) { log.AppendLine("Identidad: " + e.Message); }
    }

    private static void Escribir(StringBuilder s)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Informe, s.ToString());
        }
        catch { }
    }

    public static void Construir(bool pc, bool apk)
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("Build " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        log.AppendLine("Escenas: " + escenas.Length);
        ConstructorSupervivencia.Asegurar(log);
        ConstructorAtlas.Asegurar(log);
        AplicarIdentidad(log);
        BuildTarget original = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup grupoOriginal = BuildPipeline.GetBuildTargetGroup(original);

        if (pc)
        {
            log.AppendLine("PC: construyendo...");
            Escribir(log);
            // Construcción limpia: los archivos viejos de la carpeta (por ejemplo los punteros de Git LFS que venían en el
            // repositorio) Unity los da por "al día" y no los reemplaza, y el juego no arranca.
            string carpetaPc = Path.GetDirectoryName(RutaPc);
            try { if (Directory.Exists(carpetaPc)) Directory.Delete(carpetaPc, true); }
            catch (Exception e) { log.AppendLine("PC: no se pudo limpiar la carpeta: " + e.Message); }
            Directory.CreateDirectory(carpetaPc);
            BuildPlayerOptions op = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = RutaPc,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            Resultado("PC", BuildPipeline.BuildPlayer(op), log);
            Escribir(log);
        }

        if (apk)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                log.AppendLine("APK: ERROR - falta el módulo 'Android Build Support' (con OpenJDK y Android SDK & NDK) en Unity Hub.");
            }
            else
            {
                log.AppendLine("APK: construyendo (la primera vez tarda varios minutos)...");
                Escribir(log);
                Directory.CreateDirectory(Path.GetDirectoryName(RutaApk));
                // El identificador por defecto (com.DefaultCompany.2D-URP) no es válido en Android.
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, IdAndroid);
                PlayerSettings.Android.forceInternetPermission = true;   // multijugador por Wi-Fi
                EditorUserBuildSettings.buildAppBundle = false;
                BuildPlayerOptions op = new BuildPlayerOptions
                {
                    scenes = escenas,
                    locationPathName = RutaApk,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                };
                Resultado("APK", BuildPipeline.BuildPlayer(op), log);
            }
            Escribir(log);
        }

        if (EditorUserBuildSettings.activeBuildTarget != original)
            EditorUserBuildSettings.SwitchActiveBuildTarget(grupoOriginal, original);
        // Carpetas de depuración de Burst: no se distribuyen.
        foreach (string d in new[] { Carpeta + "/PC/Demoledor Unifranz_BurstDebugInformation_DoNotShip",
                                     Carpeta + "/Android/Demoledor Unifranz_BurstDebugInformation_DoNotShip" })
        {
            try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch { }
        }
        log.AppendLine("FIN");
        Escribir(log);
        Debug.Log("[Builds]\n" + log);
    }

    private static void Resultado(string nombre, BuildReport r, StringBuilder log)
    {
        BuildSummary s = r.summary;
        log.AppendLine(nombre + ": " + s.result + "  (" + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " +
                       s.totalTime.TotalSeconds.ToString("0") + " s, errores " + s.totalErrors + ")");
        if (s.result != BuildResult.Succeeded)
        {
            foreach (BuildStep paso in r.steps)
                foreach (BuildStepMessage m in paso.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log.AppendLine("  " + m.content);
        }
    }
}
