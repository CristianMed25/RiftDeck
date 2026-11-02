# RiftDeck

## Sistema de combate

La rama `CombatSistem` incorpora el combate sobre los contratos existentes:

- `RiftDeck.Partida.GestorPartida` mantiene los dos jugadores locales, la ronda y
  los puntos. `ControladorTurnos` sigue siendo el responsable de cubrir la
  pantalla, alternar el jugador activo y ejecutar el callback posterior al
  cambio.
- La UI existente usa `RiftDeck.UI.CardView` y `ManoCartas`, no `CartaUI`.
  `ManoCartas.PidioElegir` es el evento de selección. `CardView.BindCarta`
  conserva el render actual y expone la `CartaSO` seleccionada. `CartaUI` es
  un adaptador opcional para prefabs nuevos.
- `ControladorCombate` escucha esa selección, llama a `CombatEngine`, suma el
  resultado al `GestorPartida`, actualiza `JugadorModel`, mueve la carta a la
  zona de jugada y solicita el cambio de turno. El evento
  `puntuacionActualizada` permite conectar el HUD; `uiRefrescada` se ejecuta
  después de cambiar el turno.

El cálculo no depende de `MonoBehaviour` ni de la UI:

```text
Puntos = redondeo(valorBase * multiplicadorDeResistencia)
```

### Prueba rápida en Unity

1. En `Assets/ScriptableObjects/`, crea una carta con
   **Create > RiftDeck > Combate > Carta**. Configura `id`, `nombreCarta`,
   `valorBase`, `tipoDano` y su ilustración.
2. Crea un asset **Create > RiftDeck > Combate > Resistencias**. El arreglo
   serializado sigue el orden `Cortante`, `Perforante`, `Impacto`,
   `Elemental`; usa, por ejemplo, `2`, `1` y `0.5` para debilidad, neutral y
   resistencia.
3. En la escena Combate, conserva `InicializadorPartida` y
   `ControladorTurnos` de `feature/game-flow`. Añade `ControladorCombate`,
   asigna `ManoCartas`, `ControladorTurnos` y el asset de resistencias.
4. En cada `CardView`, usa `CartaUI` para asignar el `CartaSO` y `TiposDano`,
   o llama `BindCarta` desde el código que reparte la mano.
5. Conecta `puntuacionActualizada` al HUD y `uiRefrescada` al método que
   redibuja la mano/puntuación. Inicia la escena, selecciona una carta y
   confirma que se muestra el pase de dispositivo, cambia el jugador y se
   refleja el puntaje en la ronda actual.

Roguelike táctico de cartas para 2 jugadores en el mismo teléfono. Unity **6000.6.7f1**, Android, solo horizontal, pixel art a 640×360 (escala ×3).

Abrir el proyecto con esa versión exacta de Unity. Las escenas están en `Assets/Scenes/` y se juega desde **Menu**.

## Para el integrante 1: lo que hay que saber para hacer el combate real

Hoy el combate lo mueve **`CombateFalso`** (`Assets/Scripts/UI/CombateFalso.cs` y `CombateFalso.Prismaticos.cs`), con datos de prueba. La idea es que tu sistema lo reemplace y que **las pantallas no cambien**: todo lo visual ya está separado en componentes que solo reciben datos.

### 1. Los datos que entiende la UI
| Clase | Qué es | Cómo conectarla con lo tuyo |
|---|---|---|
| `DatosCarta` (struct) | Nombre, `costo`, `dano`, `tipo` (id del tipo de daño) y si es especial (arcano, hechizo, arma, póker) | Convierte tu `CardData` a esto **en un solo lugar**. La carta se dibuja con `CardView.Bind(datos, tipos)` |
| `DatosEnemigo` (ScriptableObject, en `Assets/Datos/Enemigos/`) | Nombre, nivel, vida, `resistencias` (tipo + multiplicador), habilidades y animaciones | Igual con tu `EnemyData`. Las resistencias se ven solas: `x2 DÉBIL`, `x1 NORMAL`, `x0.5 RESISTE` |
| `TiposDano` (`Assets/Datos/TiposDano.asset`) | Lista de tipos: id, nombre, color e ícono | La UI aguanta **N tipos**. Si agregas uno (fuego, hielo...), crea la fila aquí y sale en las cartas, las resistencias y el Perfil |
| `DatosEstado` (en `Assets/Datos/Estados/`) | Buffs y debuffs del enemigo y de las cartas | Ver la tabla de abajo. Se agregan como assets, sin tocar las pantallas |

### 2. Lo que hay que reemplazar de `CombateFalso`
- **Fórmula del daño:** `Puntos(DatosCarta)` (hoy: daño × resistencia × 10 × bonos × estados del enemigo) y `DanoJugada(...)` en `CombateFalso.Prismaticos.cs` (combos de hechizos, armas y póker).
- **Costo:** se paga **`CardView.Costo`** (el de la carta más sus estados), no `DatosCarta.costo`. El número del costo **no** va pintado en el arte: lo dibuja `NumeroPixel` con los sprites de `Art/UI/Cifras/` (salen de `tools/cifras.py`). Si un estado lo sube, se ve en rojo sobre la gema oscura; si lo baja, en menta.
- **Texto del detalle:** `Describir(CardView)` arma lo que sale al pararse sobre una carta. Lo pide el delegado `ManoCartas.Descripcion`.
- **Maná (estilo Legends of Runeterra):** en el turno N hay N de maná (tope 10) y lo que sobra pasa a 3 huecos de reserva. Se muestra con `ManaBar.Mostrar(mana, maxTurno, reserva, costoElegido)`.

### 3. Lo que tu código puede llamar para que se vea
| Llamada | Qué se ve |
|---|---|
| `EnemyPanel.Mostrar(datos, tipos)` / `MostrarVida(actual, max)` | El enemigo, su nivel, sus resistencias y la barra de vida |
| `EnemyPanel.Impacto(dano, color, donde, efecto)` | Un golpe: sacudida, número que choca "contra el vidrio" y efecto (bala, explosión, fuego, hielo o rayo) |
| `EnemyPanel.RecibirGolpe(0, color, true)` | El golpe bloqueado ("Bloqueado") |
| `EnemyPanel.Actuar("ataque" / "bloqueo" / "golpe")`, `Morir()`, `Entrar()` | Las animaciones del enemigo |
| `EnemyPanel.MostrarEstados(lista, nuevo)` + `Flechas(esBuff)` + `Anunciar(estado)` | La fila de íconos de estado, las flechas que suben o bajan y el nombre del estado subiendo en su color |
| `EnemyPanel.Curar(cantidad, desde, hasta, max)` | Cruces verdes, "+N" en menta y la barra que se llena |
| `EnemyPanel.Lanzar(estado)` + `CardView.RecibirEstado(estado, cuadros)` | El enemigo ataca, grita el estado y el efecto cae en cada carta |
| `ManoCartas.Agregar(carta)`, `CartasEnMano()`, `CartasEnZona()` | Repartir cartas (salen boca abajo del mazo) y leer la jugada |
| `PlayerHUD.Mostrar(jugador, vida, puntos, activo)` | Vida y puntos de cada jugador |

### 4. Lo que falta definir contigo
- Los tipos de daño definitivos. El enunciado dice 4 (cortante, perforante, impacto y elemental), y los hechizos ya usan fuego, hielo y rayo como "elementos".
- Los buffs y debuffs reales. Como referencia sirve **Final Fantasy Brave Exvius**: resistencias por elemento, buffs y debuffs con turnos, "breaks" que bajan las resistencias e imbuir elemento.
- Todos los números de este README (vida, daño, costos y porcentajes) son **provisionales**.
- El integrante 3 ya subió `GestorPartida` (turnos, rondas y puntos, en `Assets/Scripts/Partida/`). Hay que acordar quién le pasa los puntos (`SumarPuntos`) y quién avanza la ronda.

## Efectos de estado (buffs y debuffs)

Todos funcionan en el combate de prueba. Cada uno es un asset `DatosEstado` en `Assets/Datos/Estados/` con su ícono de Spire of Ash. Al pararse sobre el ícono del enemigo, o sobre una carta afectada, se ve qué hace y cuánto dura.

| Estado | Tipo | Va en | Qué hace | Dura | Quién lo pone | Animación | Texto en combate |
|---|---|---|---|---|---|---|---|
| **Escudo** | Buff | Enemigo | Anula todo el daño | 1 turno | Caballero, al pasar turno (50 %) | Animación BLOCK, flechas que suben e ícono de escudo | "ESCUDO" en menta, etiqueta ESCUDO, aviso "se protege", "¡Este turno lo bloquea!" en el detalle y "Bloqueado" al pegarle |
| **Defensa mayor** | Buff | Enemigo | Recibe la mitad del daño (×0.5) | Hasta el final del combate | Caballero, la 1.ª vez que su vida llega a 0: se cura **toda la vida** y gana el buff | Cruces verdes, la barra se llena y flechas que suben | "+1.500" en menta, "DEFENSA MAYOR" y aviso "¡NO CAE!" |
| **Vulnerable** | Debuff | Enemigo | Recibe +25 % de daño (×1.25) | 2 turnos | Hechizos de hielo (Escarcha, Tormenta y Cataclismo) | Flechas que bajan | "VULNERABLE" en rosa; el detalle de la carta muestra "estados x1.25" |
| **Quemadura** | Debuff | Enemigo | Pierde el 5 % de su vida máxima cada vez que se pasa el turno (no lo puede matar) | 3 turnos | Hechizos de fuego (Ascua, Supernova y Cataclismo) | Flechas que bajan; al pasar turno, llama y número | "QUEMADURA" en rosa |
| **Penitencia** | Debuff | Cartas | **Todas** las cartas que hay en la mano en ese momento cuestan +1 | Hasta el final del combate (o hasta jugar la carta) | Caballero, **una sola vez**: la primera vez que su vida baja a la mitad (por una jugada o por la Quemadura) | Ataque del Caballero con destellos rojos, flechas rojas sobre cada carta y el costo que salta y queda en rojo sobre la gema oscura | "PENITENCIA" en rojo sobre el Caballero, la pista "Penitencia: N cartas +1 de costo" y, en la carta, "Penitencia = +1 de costo · Dura hasta el final del combate" |

- Las cartas que llegan a la mano **después** de la Penitencia no la tienen: al pararse sobre ellas solo sale su descripción.
- Como el combate de prueba reparte una mano nueva cada turno, las cartas con Penitencia **vuelven a la mano de ese jugador cada turno**, todavía en rojo, hasta que las juegue o muera el Caballero. Las especiales (hechizos, armas y Arsenal) también se quedan marcadas.
- El umbral se cambia con `umbralEstadoCartas` en `Caballero.asset` (0.5 = la mitad de la vida).
- **Agregar un estado nuevo:** crea un asset con *Create > RiftDeck > Estado*. Si usa campos que ya existen (`danoRecibido`, `anulaDano`, `danoPorTurno` o `costoExtra`), funciona sin código; solo hay que decidir quién lo pone.

![El Caballero lanza Penitencia](Docs/capturas/penitencia_lanzar.png)
![Carta con Penitencia](Docs/capturas/penitencia_carta.png)
![Toda la mano con Penitencia](Docs/capturas/penitencia_todas.png)
![Carta sin estados](Docs/capturas/carta_sin_estados.png)
![Quemadura](Docs/capturas/quemadura.png)
![El Caballero se cura toda la vida](Docs/capturas/caballero_curar.png)
![No cae](Docs/capturas/caballero_no_cae.png)
![Explicación de un estado](Docs/capturas/estado_tooltip.png)

### Cifras del costo
Son blancas con contorno de Tinta y el color lo pone el código, así que no hace falta un sprite por color ni por carta. Las grandes van en la gema de las cartas normales y las chicas en el anillo de las cartas de Spire.

![Cifras](Docs/capturas/cifras.png)

## Pantallas

| Escena | Qué hay |
|---|---|
| `Menu` | Splash (solo la primera vez), menú con JUGAR, TIENDA, PERFIL, Configuración y Créditos, y el oro |
| `Modo` | Duelo local (4 rondas) o Partida rápida (1 ronda) |
| `Lobby` | "¡Duelo listo!": J1 contra J2, la semilla compartida, el primer enemigo y la cuenta 3-2-1 |
| `Perfil` | Estadísticas de cada jugador en este teléfono y los últimos duelos |
| `Combate` | La partida: enemigos por ronda, mano de cartas, maná con reserva, aumentos y arcanos |
| `Tienda` | Cosméticos con el oro del juego (dorsos, tapetes y efectos); sin dinero real |
| `Configuracion` | Sonido, vibración, calidad, velocidad de animación, nombres de los jugadores, cómo jugar y borrar progreso |
| `Creditos` | Equipo, arte de terceros con su autor, fuentes y universidad |
| `Resultados` | Ganador, puntos por ronda, oro ganado, REVANCHA y MENÚ |

"Pasa el dispositivo" no es una escena: es una pantalla que tapa el `Combate` entre turnos. La lógica de turnos y de fin de partida es del integrante 3. Aquí solo están las pantallas con nuestro estilo, conectadas a `CombateFalso` mientras tanto.

### Splash y menú
![Splash](Docs/capturas/splash.png)
![Menú](Docs/capturas/menu.png)

### Modo
JUGAR lleva aquí. La opción elegida lleva marco Oro y la etiqueta ELEGIDO.

![Modo](Docs/capturas/modo.png)

### Preparación del duelo (Lobby)
Es la versión local de la pantalla 5 del mockup. Los dos jugadores reciben las mismas cartas y enemigos porque comparten la semilla (el código #XXX-XXX). El enemigo que se muestra es el primero del combate. Cuenta 3, 2, 1 y empieza.

![Lobby](Docs/capturas/lobby.png)

### Perfil
Victorias, porcentaje ganado, récord y estilo de daño (qué tipo usa más) de cada jugador, y los últimos 3 duelos. Todo se guarda en el teléfono (`Estadisticas`).

![Perfil](Docs/capturas/perfil.png)

La pantalla 7 del mockup (Sabotaje) no se hizo porque en el juego local no hay mecánica de sabotaje.

### Combate
Pasa el dedo por la mano para ver el detalle de cada carta (daño contra el enemigo actual). Arrastra o toca las cartas para ponerlas en la zona y pulsa JUGAR.

![Combate](Docs/capturas/combate.png)

Cada enemigo tiene su gimmick. El Caballero puede bloquear todo el daño del turno siguiente:

![Caballero con escudo](Docs/capturas/caballero_escudo.png)

Las cartas salen del mazo boca abajo, con el dorso equipado en la Tienda, y se voltean al llegar a la mano. Al jugar una, viene hacia la cámara (al centro y al doble) y baja a su lugar en la zona. Las dos animaciones están tomadas del Deckbuilder Kit.

![Reparto de cartas](Docs/capturas/carta_reparto.png)
![Carta hacia la cámara](Docs/capturas/carta_presentar.png)

### Pasa el dispositivo
Al pulsar PASAR TURNO, el enemigo actúa y la pantalla se tapa por completo para que no se vea la mano del otro. El nombre va en el color de quien recibe el teléfono y la partida sigue cuando toca ESTOY LISTO.

![Pasa el dispositivo](Docs/capturas/pasa_dispositivo.png)

### Selección de aumento (estilo TFT)
Hay 3 por partida (plata, oro y prismático, el último siempre prismático) y los dos jugadores reciben las mismas opciones. El prismático tiene un relleno iridiscente con relieve y se inclina con el dedo.

![Elegir aumento](Docs/capturas/aumentos.png)

### Arcanos mayores (aumento prismático)
Llega un tarot por turno. Si juegas 1, duplica el daño de su tipo. Si juegas 3, forman un trío con bono permanente (máximo 5 tríos).

![Arcano en la mano](Docs/capturas/arcano_en_mano.png)
![Trío de arcanos](Docs/capturas/trio_arcano.png)

### Otros prismáticos y Todo o nada
- **Hechizos prohibidos:** usan el marco y las ilustraciones de Spire of Ash, con el borde dorado en prismático con relieve (refleja la luz). Cada turno hay un 12.5 % de robar un hechizo, y sube un 2.5 % por cada turno sin suerte hasta el 25 %. Ahí sale el 5 o el 6 (85/15), y si tampoco sale, el turno siguiente llega seguro el Cataclismo. Cada elemento distinto en la jugada duplica el daño.
- **RAAWRMERICA!!:** esconde 3 armas en el mazo. Cada arma jugada se suma al Arsenal, que con 5 suelta una lluvia de balas y con 7 una de misiles. Si juntas las 6 armas distintas, cae una bomba nuclear con hongo. Quien le hace el 20 % de la vida a un enemigo gana un arma.
- **Póker:** la mano pasa a ser de cartas de póker y el daño sale de la mano que armes (pareja, color, full...). Cada palo es un tipo de daño. A veces sale el comodín (prismático): vale por la carta que mejor complete la mano.
- **Todo o nada (oro):** jugar toda la mano o pasar sin jugar sube el multiplicador de daño +0.2 cada vez.

Los marcos son de Deckbuilder Kit Free y Pixel Cards Lite.

![Cartas especiales en la mano](Docs/capturas/prismaticos_cartas.png)
![Lluvia de misiles del Arsenal](Docs/capturas/arsenal.png)
![Mano de póker](Docs/capturas/poker.png)

### Lista de aumentos
Los marcos de plata y oro son de metal pulido (shader `UIMetal`, materiales `Plata` y `Oro`). Los prismáticos usan el arcoíris (`UIPrismatico`).

| Calidad | Aumento | Qué hace |
|---|---|---|
| Plata | Filo afilado | +10 % de daño con todas tus cartas |
| Plata | Mano grande | Robas 1 carta más en cada mano |
| Plata | Alcancía | Empiezas cada turno con +1 de reserva |
| Oro | Golpe crítico | Cada jugada tiene 25 % de probabilidad de hacer el doble |
| Oro | Maná rápido | Tu maná máximo sube de 2 en 2 |
| Oro | Especialista | +30 % de daño con el tipo al que el enemigo es débil |
| Oro | Todo o nada | Jugar toda la mano o pasar sin jugar: +0.2 al multiplicador, acumulable |
| Prismático | Arcanos mayores | Un arcano por turno. 3 juntos: bono permanente (máx. 5). 1 solo: efecto rápido |
| Prismático | Hechizos prohibidos | 12.5 % por turno de robar un hechizo (+2.5 % por turno sin suerte). Cada elemento distinto x2 |
| Prismático | RAAWRMERICA!! | 3 de las 6 armas van a tu mazo. Arsenal: 5 balas, 7 misiles, las 6 distintas = bomba nuclear |
| Prismático | Póker | La mano pasa a ser de póker y el daño sale de la mano que armes. Puede salir el comodín |

**Total: 11** (3 de plata, 4 de oro y 4 prismáticos).

#### ¿Cuántos hacen falta para que haya variedad?
En cada partida hay 3 ofertas, una de cada calidad, y cada oferta muestra 3 aumentos de esa calidad. Con *n* aumentos en una calidad:

| *n* por calidad | Tríos distintos posibles | Cada aumento sale en… | Misma oferta que la partida anterior |
|---|---|---|---|
| 3 | 1 | todas las partidas | siempre |
| 4 | 4 | 3 de cada 4 partidas | 25 % |
| 5 | 10 | 3 de cada 5 partidas | 10 % |
| **6** | **20** | **1 de cada 2 partidas** | **5 %** |
| 8 | 56 | 3 de cada 8 partidas | 1.8 % |

- **Mínimo recomendado: 6 por calidad, 18 en total.** Así cada aumento sale en la mitad de las partidas y casi nunca se repite la misma oferta.
- **Faltan 7:** 3 de plata (hoy la oferta de plata es siempre la misma), 2 de oro y 2 prismáticos.
- Con 8 por calidad (24 en total) ya se siente como TFT, donde no sabes qué te va a tocar.

### Tienda
![Tienda](Docs/capturas/tienda.png)

Comprar pide un segundo toque. Si te falta oro, te avisa cuánto:

![Comprar en la tienda](Docs/capturas/tienda_compras.png)

### Resultados
Se abre al terminar la ronda 4. Gana quien sume más puntos en total. La tabla dice quién ganó cada ronda con su nombre (no solo con el color) y la partida da +60 de oro para la Tienda.

![Resultados](Docs/capturas/resultados.png)

### Configuración
Todo se guarda en el teléfono (`Ajustes`, con PlayerPrefs). Los nombres de los jugadores (hasta 8 letras) salen en el HUD, en "Pasa el dispositivo" y en Resultados. La velocidad x2 acelera las animaciones del combate. "Borrar progreso" pide un segundo toque y solo borra el oro y los cosméticos.

![Configuración](Docs/capturas/config.png)
![Cómo jugar](Docs/capturas/config_ayuda.png)

### Créditos
![Créditos](Docs/capturas/creditos.png)

## Notas para el equipo
- Los valores de cartas, enemigos, aumentos y precios son **provisionales** (`CombateFalso` usa datos de prueba) hasta que el integrante 1 defina el combate.
- El oro se guarda en el teléfono (`Inventario`). Resultados ya suma el de cada partida con `ResultadoPartida.CobrarOro()`.
- **Para el integrante 3:** al terminar la partida, llama a `ResultadoPartida.Guardar(puntosJ1PorRonda, puntosJ2PorRonda)` y carga la escena `Resultados`. Para cambiar de turno, usa `PasaDispositivo.Mostrar(...)`: tapa la pantalla, corre tu cambio de mano ya tapada y espera a ESTOY LISTO. Mira cómo lo hace `CombateFalso.CambioDeTurno`.
- El arte de terceros está en `Assets/Art/Terceros/`. Varios packs no permiten redistribuir los archivos sueltos, así que **Claude sugiere hacer privado el repositorio** para no arriesgarnos a un reclamo por copyright.
