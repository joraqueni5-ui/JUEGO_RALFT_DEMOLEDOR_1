"""Genera Assets/Scenes/Universidad.unity a partir del arte exportado.

La escena contiene:
  - Main Camera (ortográfica, encuadra el edificio)
  - Global Light 2D
  - Fondo / Interior / Suelo
  - Edificio con 6x4 = 24 bloques (cada uno con BloqueEdificio)
  - Demoledor y Muneco (personajes jugables)
  - GestorJuego (HUD, marcador, reinicio)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_assets import ROOT, guid_for, write_text_meta  # noqa: E402

ARTE = os.path.join(ROOT, "Assets", "Arte")
ESCENA = os.path.join(ROOT, "Assets", "Scenes", "Universidad.unity")

COLUMNAS = 12
FILAS = 8
ANCHO_BLOQUE = 0.6   # 48 px / 80 ppu
ALTO_BLOQUE = 0.5    # 40 px / 80 ppu
EDIFICIO_X = 2.0
EDIFICIO_Y = 0.0

CAMARA_X = 5.6
CAMARA_Y = 2.2
CAMARA_TAMANO = 3.2

MATERIAL_SPRITE = "{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}"
GUID_CAMARA_URP = "a79441f348de89743a2939f4d699eac1"
GUID_LUZ_2D = "073797afb82c5a1438f328866b10b3f0"


def png(ruta_relativa):
    """Referencia a un sprite importado (fileID 21300000 = sprite único)."""
    ruta = os.path.join(ARTE, ruta_relativa)
    if not os.path.exists(ruta):
        raise FileNotFoundError(ruta)
    return "{fileID: 21300000, guid: %s, type: 3}" % guid_for(ruta)


def script(nombre):
    ruta = os.path.join(ROOT, "Assets", "Scripts", nombre + ".cs")
    if not os.path.exists(ruta):
        raise FileNotFoundError(ruta)
    guid = write_text_meta(ruta)
    return "{fileID: 11500000, guid: %s, type: 3}" % guid


class Nodo:
    def __init__(self, escena, nombre, padre=None, pos=(0.0, 0.0, 0.0), escala=(1.0, 1.0, 1.0),
                 tag="Untagged", rot_z=0.0):
        self.escena = escena
        self.nombre = nombre
        self.tag = tag
        self.pos = pos
        self.escala = escala
        self.gid = escena.nuevo_id()
        self.tid = escena.nuevo_id()
        self.componentes = [self.tid]
        self.hijos = []
        self.padre = padre
        self.rot_z = rot_z
        self.docs = []
        if padre is not None:
            padre.hijos.append(self.tid)
        else:
            escena.raices.append(self.tid)

    def componente(self, texto):
        self.docs.append(texto)
        return self

    # ---------- componentes ----------
    def sprite(self, sprite_ref, orden=0, flip_x=False, color=(1, 1, 1, 1)):
        cid = self.escena.nuevo_id()
        self.componentes.append(cid)
        self.docs.append(f"""--- !u!212 &{cid}
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {MATERIAL_SPRITE}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: {orden}
  m_Sprite: {sprite_ref}
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_FlipX: {1 if flip_x else 0}
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 1, y: 1}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
""")
        return self

    def mono(self, script_ref, campos=""):
        cid = self.escena.nuevo_id()
        self.componentes.append(cid)
        extra = "\n" + campos.rstrip("\n") if campos else ""
        self.docs.append(f"""--- !u!114 &{cid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {script_ref}
  m_Name: 
  m_EditorClassIdentifier: {extra}
""")
        return self

    def rigidbody(self, masa=1.0, gravedad=3.0):
        cid = self.escena.nuevo_id()
        self.componentes.append(cid)
        self.docs.append(f"""--- !u!50 &{cid}
Rigidbody2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_BodyType: 0
  m_Simulated: 1
  m_UseFullKinematicContacts: 0
  m_UseAutoMass: 0
  m_Mass: {masa}
  m_LinearDamping: 0
  m_AngularDamping: 0.05
  m_GravityScale: {gravedad}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_Interpolate: 1
  m_SleepingMode: 1
  m_CollisionDetection: 1
  m_Constraints: 4
""")
        return self

    def caja(self, tamano, offset=(0.0, 0.0), sprite_size=(1.0, 1.0), es_trigger=False):
        cid = self.escena.nuevo_id()
        self.componentes.append(cid)
        self.docs.append(f"""--- !u!61 &{cid}
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: {1 if es_trigger else 0}
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {{x: {offset[0]}, y: {offset[1]}}}
  m_SpriteTilingProperty:
    border: {{x: 0, y: 0, z: 0, w: 0}}
    pivot: {{x: 0.5, y: 0.5}}
    oldSize: {{x: {sprite_size[0]}, y: {sprite_size[1]}}}
    newSize: {{x: {sprite_size[0]}, y: {sprite_size[1]}}}
    adaptiveTilingThreshold: 0.5
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  serializedVersion: 2
  m_Size: {{x: {tamano[0]}, y: {tamano[1]}}}
  m_EdgeRadius: 0
""")
        return self

    # ---------- salida ----------
    def doc_gameobject(self):
        comps = "\n".join("  - component: {fileID: %d}" % c for c in self.componentes)
        return f"""--- !u!1 &{self.gid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}
  m_Layer: 0
  m_Name: {self.nombre}
  m_TagString: {self.tag}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""

    def doc_transform(self):
        if self.hijos:
            hijos = "".join("\n  - {fileID: %d}" % h for h in self.hijos)
        else:
            hijos = " []"
        padre = self.padre.tid if self.padre is not None else 0
        return f"""--- !u!4 &{self.tid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {self.pos[0]}, y: {self.pos[1]}, z: {self.pos[2]}}}
  m_LocalScale: {{x: {self.escala[0]}, y: {self.escala[1]}, z: {self.escala[2]}}}
  m_ConstrainProportionsScale: 0
  m_Children:{hijos}
  m_Father: {{fileID: {padre}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {self.rot_z}}}
"""


class Escena:
    def __init__(self):
        self.nodos = []
        self.raices = []
        self.contador = 1000000

    def nuevo_id(self):
        self.contador += 1
        return self.contador

    def nodo(self, *args, **kwargs):
        n = Nodo(self, *args, **kwargs)
        self.nodos.append(n)
        return n

    def escribir(self, ruta):
        cabecera = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
"""
        partes = [cabecera]
        for nodo in self.nodos:
            partes.append(nodo.doc_gameobject())
            partes.append(nodo.doc_transform())
            partes.extend(nodo.docs)

        raices = "".join("\n  - {fileID: %d}" % r for r in self.raices)
        partes.append("""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:%s
""" % raices)

        # Sin líneas en blanco entre documentos: Unity agrega un elemento null
        # al final de una lista si el array queda seguido de una línea vacía.
        texto = "".join(p.rstrip("\n") + "\n" for p in partes)

        os.makedirs(os.path.dirname(ruta), exist_ok=True)
        with open(ruta, "w", encoding="utf-8", newline="\n") as f:
            f.write(texto)


def crear_camara(esc):
    cam = esc.nodo("Main Camera", pos=(CAMARA_X, CAMARA_Y, -10.0), tag="MainCamera")
    cid = esc.nuevo_id()
    cam.componentes.append(cid)
    cam.docs.append(f"""--- !u!20 &{cid}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam.gid}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.55, g: 0.68, b: 0.82, a: 1}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 34
  orthographic: 1
  orthographic size: {CAMARA_TAMANO}
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 0
  m_HDR: 1
  m_AllowMSAA: 0
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 0
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
""")
    aid = esc.nuevo_id()
    cam.componentes.append(aid)
    cam.docs.append(f"""--- !u!81 &{aid}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam.gid}}}
  m_Enabled: 1
""")
    cam.mono("{fileID: 11500000, guid: %s, type: 3}" % GUID_CAMARA_URP, """  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {fileID: 0}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 0
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_StopNaN: 0
  m_Dithering: 0
  m_ClearDepth: 1
  m_AllowXRRendering: 1
  m_AllowHDROutput: 1
  m_UseScreenCoordOverride: 0
  m_ScreenSizeOverride: {x: 0, y: 0, z: 0, w: 0}
  m_ScreenCoordScaleBias: {x: 0, y: 0, z: 0, w: 0}
  m_RequiresDepthTexture: 0
  m_RequiresColorTexture: 0
  m_TaaSettings:
    m_Quality: 3
    m_FrameInfluence: 0.1
    m_JitterScale: 1
    m_MipBias: 0
    m_VarianceClampScale: 0.9
    m_ContrastAdaptiveSharpening: 0
  m_Version: 2""")
    return cam


def crear_luz(esc):
    luz = esc.nodo("Global Light 2D")
    luz.mono("{fileID: 11500000, guid: %s, type: 3}" % GUID_LUZ_2D, """  m_ComponentVersion: 2
  m_LightType: 4
  m_BlendStyleIndex: 0
  m_FalloffIntensity: 0.5
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_Intensity: 1
  m_LightVolumeIntensity: 1
  m_LightVolumeEnabled: 0
  m_ApplyToSortingLayers: 00000000
  m_LightCookieSprite: {fileID: 0}
  m_DeprecatedPointLightCookieSprite: {fileID: 0}
  m_LightOrder: 0
  m_AlphaBlendOnOverlap: 0
  m_OverlapOperation: 0
  m_NormalMapDistance: 3
  m_NormalMapQuality: 2
  m_UseNormalMap: 0
  m_ShadowsEnabled: 0
  m_ShadowIntensity: 0.75
  m_ShadowSoftness: 0
  m_ShadowSoftnessFalloffIntensity: 0.5
  m_ShadowVolumeIntensityEnabled: 0
  m_ShadowVolumeIntensity: 0.75
  m_LocalBounds:
    m_Center: {x: 0, y: -0.00000011920929, z: 0}
    m_Extent: {x: 0.9985302, y: 0.99853027, z: 0}
  m_PointLightInnerAngle: 360
  m_PointLightOuterAngle: 360
  m_PointLightInnerRadius: 0
  m_PointLightOuterRadius: 1
  m_ShapeLightParametricSides: 5
  m_ShapeLightParametricAngleOffset: 0
  m_ShapeLightParametricRadius: 1
  m_ShapeLightFalloffSize: 0.5
  m_ShapeLightFalloffOffset: {x: 0, y: 0}
  m_ShapePath:
  - {x: -0.5, y: -0.5, z: 0}
  - {x: 0.5, y: -0.5, z: 0}
  - {x: 0.5, y: 0.5, z: 0}
  - {x: -0.5, y: 0.5, z: 0}""")
    return luz


def refs(lista):
    return "".join("\n  - {fileID: 21300000, guid: %s, type: 3}" % g for g in lista)


def bloque_tiene_contenido(fila, columna):
    """Los bloques de cielo (arriba del techo) no se crean: no son rompibles."""
    from PIL import Image
    ruta = os.path.join(ARTE, "Edificio", "bloque_%d_%d.png" % (fila, columna))
    if not os.path.exists(ruta):
        return False
    return Image.open(ruta).getbbox() is not None


def creador(prefijo, cantidad):
    guids = []
    for i in range(cantidad):
        ruta = os.path.join(ARTE, prefijo + "_%d.png" % i)
        if not os.path.exists(ruta):
            raise FileNotFoundError(ruta)
        guids.append(guid_for(ruta))
    return guids


def construir():
    esc = Escena()
    crear_camara(esc)
    crear_luz(esc)

    # Fondo de la calle (12 x 6.75 unidades, cubre el encuadre).
    esc.nodo("Fondo", pos=(CAMARA_X, CAMARA_Y, 0.0)) \
        .sprite(png("Nivel/fondo.png"), orden=-50)

    # Interior oscuro del edificio (se ve por los huecos).
    esc.nodo("Interior", pos=(EDIFICIO_X + COLUMNAS * ANCHO_BLOQUE * 0.5,
                              EDIFICIO_Y + FILAS * ALTO_BLOQUE * 0.5, 0.0)) \
        .sprite(png("Nivel/interior.png"), orden=0)

    # Suelo (16 x 1.2 unidades, borde superior en y = 0).
    suelo = esc.nodo("Suelo", pos=(CAMARA_X, -0.6, 0.0))
    suelo.sprite(png("Nivel/suelo.png"), orden=2)
    suelo.caja(tamano=(16.0, 1.2), offset=(0.0, 0.0), sprite_size=(16.0, 1.2))

    # Edificio: rejilla de bloques hijos (bloque a bloque).
    edificio = esc.nodo("Edificio", pos=(EDIFICIO_X, EDIFICIO_Y, 0.0))
    edificio.mono(script("Edificio"), "  golpesParaRomper: 2")
    bloques = 0
    for fila in range(FILAS):
        for col in range(COLUMNAS):
            if not bloque_tiene_contenido(fila, col):
                continue
            x = ANCHO_BLOQUE * (col + 0.5)
            y = ALTO_BLOQUE * (fila + 0.5)
            bloque = esc.nodo("Bloque_%d_%d" % (fila, col), padre=edificio, pos=(x, y, 0.0))
            bloque.sprite(png("Edificio/bloque_%d_%d.png" % (fila, col)), orden=1)
            bloque.mono(script("BloqueEdificio"),
                        "  fila: %d\n" % fila +
                        "  columna: %d\n" % col +
                        "  golpesParaRomper: 2\n"
                        "  spriteAgrietado: " + png("Edificio/bloque_%d_%d_danado.png" % (fila, col)))
            bloques += 1
    print("Bloques creados:", bloques, "de", FILAS * COLUMNAS, "celdas")

    # Demoledor automático (no tiene física ni colisiones: se mueve por guion).
    demoledor = esc.nodo("Demoledor", pos=(2.3, 3.0, 0.0))
    demoledor.sprite(png("Demoledor/caminar_der_0.png"), orden=5)
    demoledor.mono(script("DemoledorAutomatico"),
                   "  caminarIzquierda:" + refs(creador("Demoledor/caminar_izq", 6)) + "\n"
                   "  caminarDerecha:" + refs(creador("Demoledor/caminar_der", 6)) + "\n"
                   "  salto:" + refs(creador("Demoledor/salto", 4)) + "\n"
                   "  golpe:" + refs(creador("Demoledor/golpe", 6)))

    # Muñeco (único personaje jugable).
    muneco = esc.nodo("Muneco", pos=(1.15, 0.0, 0.0))
    muneco.sprite(png("Muneco/caminar_0.png"), orden=5)
    muneco.rigidbody(masa=0.8, gravedad=3.0)
    muneco.caja(tamano=(0.4, 0.82), offset=(0.0, 0.41), sprite_size=(0.52, 0.87))
    muneco.mono(script("Muneco"),
                "  caminar:" + refs(creador("Muneco/caminar", 4)) + "\n"
                "  caminarEspaldas:" + refs(creador("Muneco/espalda", 4)) + "\n"
                "  martillo:" + refs(creador("Muneco/martillo", 4)))

    # Gestor del juego (HUD + reinicio).
    esc.nodo("GestorJuego").mono(script("GestorJuego"))

    esc.escribir(ESCENA)
    print("Escena escrita en", os.path.relpath(ESCENA, ROOT))
    print("Bloques:", FILAS * COLUMNAS, "| tamano bloque:", ANCHO_BLOQUE, "x", ALTO_BLOQUE)
    registrar_en_build_settings()


def registrar_en_build_settings():
    """Deja la escena como primera escena del juego (para darle Play y para el build)."""
    guid = guid_for(ESCENA)
    with open(ESCENA + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write("fileFormatVersion: 2\nguid: %s\nDefaultImporter:\n  externalObjects: {}\n"
                "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid)

    ruta = os.path.join(ROOT, "ProjectSettings", "EditorBuildSettings.asset")
    with open(ruta, "r", encoding="utf-8") as f:
        texto = f.read()

    entrada = "  - enabled: 1\n    path: Assets/Scenes/Universidad.unity\n    guid: %s\n" % guid
    if "Universidad.unity" not in texto:
        texto = texto.replace("  m_Scenes:\n", "  m_Scenes:\n" + entrada, 1)
        with open(ruta, "w", encoding="utf-8", newline="\n") as f:
            f.write(texto)
        print("Escena agregada a Build Settings")
    else:
        print("La escena ya estaba en Build Settings")


if __name__ == "__main__":
    construir()
