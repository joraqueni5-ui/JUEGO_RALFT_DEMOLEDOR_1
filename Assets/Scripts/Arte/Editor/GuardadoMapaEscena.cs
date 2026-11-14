using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Deja el mapa de una escena "ya construido": guarda las paredes delgadas como una textura (un PNG por piso), pone en
/// el mapa el dibujo de reparables, decoración, carteles, entrada y ascensor (lo mismo que haría al dar Play) y crea los
/// puntos "?". Así el piso se ve y se edita en el editor sin dar Play; al jugar, el mapa vuelve a leerse tal como quedó.
/// </summary>
public static class GuardadoMapaEscena
{
    public static void Guardar(DefinicionPiso def, MapaCenital mapa)
    {
        Physics2D.SyncTransforms();

        // 1) Paredes delgadas en una sola textura.
        Vector2Int origen;
        Texture2D textura = mapa.ComponerParedes(def, out origen);
        if (textura != null)
        {
            string ruta = RutasArte.Generado + "/paredes_piso" + def.numero + ".png";
            Sprite sprite = SpritesDerivados.Guardar(textura, ruta, false, SpriteAlignment.BottomLeft);

            Transform previo = mapa.transform.Find("ParedesDelgadas");
            if (previo != null) Object.DestroyImmediate(previo.gameObject);
            GameObject g = new GameObject("ParedesDelgadas");
            g.transform.SetParent(mapa.transform, false);
            // Se coloca por los límites del sprite (no por su pivote): la esquina inferior izquierda queda justo en la celda de origen.
            g.transform.position = new Vector3(origen.x, origen.y, 0f) - sprite.bounds.min;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -150;
            mapa.paredesGuardadas = true;
        }

        // Las baldosas de pared quedan solo para el collider (lo que se ve son las paredes delgadas).
        foreach (Tilemap tm in mapa.GetComponentsInChildren<Tilemap>())
        {
            if (tm.GetComponent<TilemapCollider2D>() == null) continue;
            TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null) tr.enabled = false;
        }

        // 2) Puntos "?" (marcas en el editor; el icono se crea al jugar).
        PuntoInformacion[] viejos = mapa.GetComponentsInChildren<PuntoInformacion>(true);
        foreach (PuntoInformacion v in viejos) Object.DestroyImmediate(v.gameObject);
        if (def.puntosInfo != null)
        {
            for (int i = 0; i < def.puntosInfo.Length; i++)
            {
                PuntoInfoPiso ficha = def.puntosInfo[i];
                if (ficha == null) continue;
                GameObject p = new GameObject("PuntoInformacion " + (i + 1));
                p.transform.SetParent(mapa.transform, false);
                p.transform.position = new Vector3(ficha.posicion.x, ficha.posicion.y, 0f);
                p.AddComponent<PuntoInformacion>().Configurar(i, ficha);
            }
        }

        // 3) Dibujo de reparables, decoración, entrada y ascensor.
        mapa.Preparar(def);
        EditorUtility.SetDirty(mapa);
    }
}
