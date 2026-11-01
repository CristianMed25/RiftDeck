# RiftDeck

Roguelike táctico de cartas para 2 jugadores en el mismo teléfono. Unity **6000.6.7f1**, Android, solo horizontal, pixel art a 640×360 (escala ×3).

Abrir el proyecto con esa versión exacta de Unity. Las escenas están en `Assets/Scenes/` y se juega desde **Menu**.

## Pantallas

| Escena | Qué hay |
|---|---|
| `Menu` | Splash (solo la primera vez), menú con JUGAR, TIENDA, Configuración y Créditos, y el oro |
| `Combate` | La partida: enemigos por ronda, mano de cartas, maná con reserva, aumentos y arcanos |
| `Tienda` | Cosméticos con el oro del juego (dorsos, tapetes y efectos); sin dinero real |

Configuración y Créditos todavía no tienen escena. Resultados y "Pasa el dispositivo" son del integrante 3.

### Splash y menú
![Splash](Docs/capturas/splash.png)
![Menú](Docs/capturas/menu.png)

### Combate
Pasa el dedo por la mano para ver el detalle de cada carta (daño contra el enemigo actual). Arrastra o toca las cartas para ponerlas en la zona y pulsa JUGAR.

![Combate](Docs/capturas/combate.png)

Cada enemigo tiene su gimmick. El Caballero puede bloquear todo el daño del turno siguiente:

![Caballero con escudo](Docs/capturas/caballero_escudo.png)

### Selección de aumento (estilo TFT)
Hay 3 por partida (plata, oro y prismático, el último siempre prismático) y los dos jugadores reciben las mismas opciones. El prismático tiene un relleno iridiscente con relieve y se inclina con el dedo.

![Elegir aumento](Docs/capturas/aumentos.png)

### Arcanos mayores (aumento prismático)
Llega un tarot por turno. Si juegas 1, duplica el daño de su tipo. Si juegas 3, forman un trío con bono permanente (máximo 5 tríos).

![Arcano en la mano](Docs/capturas/arcano_en_mano.png)
![Trío de arcanos](Docs/capturas/trio_arcano.png)

### Tienda
![Tienda](Docs/capturas/tienda.png)

Comprar pide un segundo toque. Si te falta oro, te avisa cuánto:

![Comprar en la tienda](Docs/capturas/tienda_compras.png)

## Notas para el equipo
- Los valores de cartas, enemigos, aumentos y precios son **provisionales** (`CombateFalso` usa datos de prueba) hasta que el integrante 1 defina el combate.
- El oro se guarda en el teléfono (`Inventario`). La pantalla de resultados puede sumarlo con `Inventario.GanarOro(n)`.
- El arte de terceros está en `Assets/Art/Terceros/`. Varios packs no permiten redistribuir los archivos sueltos, así que conviene que el repositorio sea privado.
