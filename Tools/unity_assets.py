"""Utilidades para generar assets de Unity (sprites PNG + .meta deterministas)."""
import hashlib
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

ALIGN_CENTRO = 0
ALIGN_ABAJO_CENTRO = 7


def guid_for(asset_path):
    """GUID estable a partir de la ruta relativa del asset."""
    rel = os.path.relpath(asset_path, ROOT).replace("\\", "/").lower()
    return hashlib.md5(rel.encode("utf-8")).hexdigest()


PNG_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: {alignment}
  spritePivot: {{x: {pivot_x}, y: {pivot_y}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

SCRIPT_META = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def sprite_id(asset_path):
    """fileID interno del sprite (un solo sprite por textura)."""
    return 21300000


def write_png(path, image, ppu=100, pivot="centro", filtro=0, verbose=True):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)
    guid = guid_for(path)
    if pivot == "centro":
        alignment, px, py = ALIGN_CENTRO, 0.5, 0.5
    else:  # abajo-centro (pies)
        alignment, px, py = ALIGN_ABAJO_CENTRO, 0.5, 0.0
    meta = PNG_META.format(guid=guid, ppu=ppu, alignment=alignment, pivot_x=px, pivot_y=py, filter=filtro)
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    if verbose:
        print(f"   {os.path.relpath(path, ROOT)}  {image.size}  guid={guid[:8]}...")
    return path, guid, sprite_id(path)


def write_text_meta(path):
    """Crea el .meta de un script con un GUID determinista (lo usa la escena)."""
    guid = guid_for(path)
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(SCRIPT_META.format(guid=guid))
    return guid
