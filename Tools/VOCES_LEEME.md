# Voces grabadas (opcional, por ejemplo con VoiceStudio)

El juego ya habla con un balbuceo por sílabas (`SonidosExtra/voz_*`). Si quieres voces reales:

1. Abre `Tools/voces_lineas.csv`: cada fila tiene el nombre de archivo, el texto y el tipo de voz
   (`personaje`, `villano` o `mini`).
2. Genera cada frase con VoiceStudio (https://github.com/debpalash/VoiceStudio) u otra herramienta de voz, en español,
   y guárdala como WAV con el nombre de la columna `archivo` (por ejemplo `como_nuevo.wav`).
3. Copia los WAV a `Assets/Resources/Voces/`.

Cuando un globo muestra una frase y existe `Resources/Voces/<archivo>.wav`, suena la grabación en lugar del balbuceo.
Si agregas frases nuevas en `Assets/Scripts/Extras/Frases.cs`, vuelve a generar la lista con el bloque de Python
que está en el historial del proyecto o escribe el nombre a mano: minúsculas, sin tildes ni signos, palabras unidas con "_".
