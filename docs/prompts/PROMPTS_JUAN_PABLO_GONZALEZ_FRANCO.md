# Historial de prompts: Juan Pablo González Franco

Rol en el TP: documentación de arquitectura (diagramas de Availability y Booking, y guías de estudio de cada API).

**Herramienta:** Claude (Anthropic)  
**Rama:** `docs/availability`

## Cómo encarar la documentación

**1.** Me tocó documentar Availability (punto i del TP: "realizar un diagrama de arquitectura de software de cada API"). ¿Tengo que esperar a que arranquen a codear? ¿Cómo se hace, es escrito o gráfico?

**2.** Soy nuevo en todo esto: explicame los pasos para documentar y mostrame un ejemplo de cómo se ve un diagrama de este estilo.

**3.** Antes del cómo, quiero entender cómo trabaja alguien que documenta y qué tipo de preguntas les puedo hacer a los desarrolladores para ir entendiendo todo.

## Diagrama de Availability: anillo azul (Frameworks and drivers)

**4.** Arranquemos el diagrama de Availability en Clean Architecture, pero quiero ir hablándolo y entender yo cómo dibujar y conectar cada cosa. Vamos de afuera hacia adentro y que cada cosa que sumemos tenga su explicación.

**5.** Te cuento cómo entiendo el flujo: el usuario consulta los asientos en el front, Availability responde por HTTP, le pregunta a Booking cuáles están ocupados y, cuando alguien quiere reservar, deja la solicitud en Azure (el broker) para que Booking la procese. Corregime y veamos qué cuadros irían en el azul.

**6.** ¿Por qué la solicitud se guarda en la misma base que los asientos? ¿No deberían ser dos bases? Ah, son dos tablas en la misma base.

**7.** El TP dice que para ver qué asientos están ocupados no hace falta login, recién al reservar. ¿El token entra acá?

**8.** Los pedidos que le llegan al controller serían ver el plano, pedir una reserva y ver el estado. Con la reserva, se la pasa al Service Bus para que Booking la tome. Corregime antes de sumarlo: siento que el controller es el puente entre lo que entra y lo que sale de la API.

**9.** Antes de seguir, ¿cómo lo dibujo? ¿Tendría que poner los métodos en cada caja?

**10.** ¿De qué tipo es ReservaController y cómo se llamaría? ¿Por qué el otro se llama Availability API Controller?

**11.** La autenticación revisa el token cuando llega un pedido de reserva y lo aprueba o lo rechaza; o sea, controla que el token sea válido. ¿Por qué la flecha del front iría primero a la autenticación si puedo ver el mapa sin reservar?

**12.** ¿La caja de autenticación JWT queda sin atributos ni métodos? La flecha iría del controller a la autenticación solo para solicitar reserva y ver el estado, no para la disponibilidad.

**13.** ¿Dónde va el authorization server? ¿Afuera, como el front? ¿Y quién crea el token?

**14.** Lo que se lee y se guarda en Availability son los asientos físicos y las solicitudes de reserva que después van al Service Bus.

**15.** ¿Hace falta una tabla de eventos si capaz hay uno solo? ¿La base de Keycloak no se dibuja? ¿Al DbContext no le llega ninguna flecha?

**16.** El repositorio tendría métodos para leer y guardar y un atributo del tipo AvailabilityDbContext. ¿Qué métodos concretos serían? ¿Cómo se llama? Su función es hacer las consultas al DbContext, que es el que describe la base, ¿no?

**17.** Booking Client: Availability le pregunta a Booking qué butacas están ocupadas y el estado de una reserva. ¿Qué atributos y métodos tiene?

**18.** Service Bus Publisher: publica la solicitud en Azure Service Bus cuando el usuario aprieta reservar. El connection string ya nos lo pasó el profe; ¿el sender lo da Azure o lo programamos nosotros?

**19.** Program conecta las clases concretas del azul con sus interfaces. ¿Cómo se llama esta clase y cómo la armó el profe? ¿Hace falta una caja Register Services con todos los métodos? Si además configura la base, el JWT y el Service Bus, ¿cómo la grafico?

## Diagrama de Availability: anillo verde (Adapters)

**20.** El verde es el traductor para que los cambios de afuera no lleguen al centro. El flujo entra por el front al controller de la API, que sería el duplicado, ¿no?

**21.** El controller del verde traduce lo que le llega del controller de la API y lo delega al caso de uso, ¿no? ¿Tiene esos métodos sin atributos?

**22.** El resultado habría que guardarlo en la base para que quede registrado y después comunicarlo.

**23.** Entonces la clase nueva es el ViewModel, con un atributo de respuesta y un método para armarla. ¿El profe llama handle al que arma el ViewModel? ¿De qué tipo son sus atributos?

**24.** Si el presenter presenta la respuesta, ¿no debería usarla el controller? ¿No le falta un atributo del presenter para leer el id, el mensaje y la fecha? ¿Por qué no lo pusimos antes?

**25.** ¿Cómo le llega al presenter el resultado del caso de uso si solo tiene el ViewModel como atributo? Ah, le llega por parámetro: en handle recibe el resultado y arma el ViewModel que después lee ReservaController. [captura de pantalla] Así lo tengo.

**26.** Se me ocurre que hace falta un traductor que decida qué datos se quedan en el camino y cuáles se comunican. Avancemos como provisorio, pero no entiendo dónde estaría la entidad de base. Volvamos al verde, que me perdí.

**27.** ¿Hay un mapper por caso de uso o uno general? En el verde quedan ReservaController, el presenter, el ViewModel y el mapper, ¿no?

**28.** ¿El repositorio no era el que consulta la base con el DbContext? ¿Tendría que tener un atributo para usar el mapper, uno por entidad?

**29.** No entiendo la parte de guardar. ¿El DbContext debería tener un atributo del repositorio? ¿No le faltan métodos, uno por entidad? ¿Por qué tengo un "guardar solicitud"?

**30.** O sea: el DbContext describe la base y el repositorio hace las consultas para leer y guardar, usando el mapper para traducir. ¿En qué caso Availability guardaría algo? Y si el estado lo tiene Booking, ¿cómo lo sabe Availability?

**31.** Los atributos de los mappers en el repositorio son privados, ¿no? ¿Y las tablas del DbContext?

**32.** Si es así, ¿tendría que dibujar todos los mappers, presenters y ViewModels de todos los casos de uso? Mantengamos un solo ejemplo, el de solicitud, y después cajitas vacías de los demás para que no quede repleto.

## Diagrama de Availability: anillo rojo (casos de uso)

**33.** Vamos al rojo con la misma dinámica, que me está sirviendo. ¿Los datos se guardan de la forma en que se procesan en los casos de uso?

**34.** El caso de uso primero tendría que chequear que no exista otra solicitud con ese código y después gestionarla. ¿No necesitaría llamar al mapper?

**35.** ¿Y el output port dónde está? No lo tengo. ¿Por qué el caso de uso llama al repositorio, al publicador y al output port?

**36.** No entiendo cómo se conecta el presenter: su handle recibe un resultado y el del interactor, un request. ¿Es un tema de nombres? Nos faltan cuadros: no me expliques cosas que no dibujamos, veamos en qué orden conviene dibujarlos.

**37.** La interfaz sirve para que ReservaController le pida algo al caso de uso, y el caso de uso la implementa. Program pone el interactor detrás del atributo del controller, mediante la interfaz: nunca se conocen directo, ¿no?

**38.** Repasemos la triple conexión: llega la solicitud, el controller llama al ejecutar de su atributo del tipo input port y, como esa interfaz la implementa el interactor, el interactor recibe la información sin que se conozcan.

**39.** La interfaz del repositorio dice cómo se piden los métodos que cumple el repositorio del azul, y el interactor puede usarlos porque tiene un atributo de ese tipo.

**40.** El interactor tiene un atributo publicador del tipo IEventPublisher, la interfaz que declara publicar solicitud, y la implementa el publisher de Azure del azul.

**41.** ¿Por qué el interactor tiene un output port como atributo pero no un input port? Ah, porque lo implementa.

**42.** El caso de uso necesitaría el ID de la butaca, la persona y el evento. ¿No debería tener un atributo RequestReservationCommand?

**43.** El resultado debería devolver el ID de la reserva. Y las solicitudes quedarían "recibidas", no "creadas", como en espera.

**44.** El mensaje lleva el ID del asiento y el evento. ¿Esto se repite en los otros casos de uso?

## Diagrama de Availability: anillo amarillo (entidades)

**45.** Availability tiene eventos, solicitudes y asientos. El evento tiene ID, nombre y capacidad, ¿o es mucho? El asiento tiene ID y evento.

**46.** ¿El asiento no debería tener un EventId en vez del evento entero? ¿Los miembros de evento y asiento son públicos?

**47.** La solicitud tiene ID, estado, asiento, evento y fecha. ¿El enum de estado puede ser una entidad?

**48.** ¿Cómo afecta esto a los DbSet del azul? No entendí lo de las entidades de base: explicámelo.

**49.** ¿Para qué serviría la row version? ¿Tiene que existir desde ahora porque viaja al Service Bus? Si en Availability no hay concurrencia optimista, ¿para qué la entidad de base? Ya la saqué.

**50.** ¿Por qué no dibujamos el punto 9? Porque es otro caso de uso. En el 8, Execute verifica el asiento, genera el ID, guarda, publica y entrega el resultado.

**51.** ¿Hace falta agregar todas las rutas en el diagrama?

## Diagrama de Booking, piso por piso (N-Layer)

**52.** La comparación de estados para saber si está confirmada o retenida va en el piso del medio.

**53.** Availability le habla a Booking de dos formas: por el broker (publica y Booking toma) y por HTTP al controller. Booking guarda la reserva para después responder el estado por HTTP.

**54.** Si falla el cobro, marcaría "pago no disponible". Las dos situaciones serían cuando le llega la solicitud y la guarda, y cuando responde.

**55.** Por el controller entra lo HTTP y por el worker lo del broker, ¿no? GetBookings le traería a quien busca un estado todas las reservas, que no le importan.

**56.** El controller es la puerta de entrada HTTP. No tiene sentido crear una reserva por HTTP si está el broker.

**57.** El worker tiene un scopeFactory de tipo IServiceScopeFactory que no veo en otras cajas, mientras el controller tiene un IBookingService. ¿Por qué?

**58.** Program: si llega un token de Availability con otra audiencia, no lo deja entrar a Booking.

**59.** ¿por qué la interfaz tiene 4 métodos y el controller 3? ¿Por qué el worker no tiene el método que aparece en IBookingService?

**60.** IBookingService es el contrato del cerebro y el worker lo usa en uno de sus métodos.

**61.** El vencimiento perezoso marca vencida una reserva recién cuando alguien la mira. Y si no hay un freno, dos pedidos al mismo asiento romperían todo: para eso está la concurrencia optimista.

**62.** ProcessReservationRequestAsync hace todo lo de reservar: idempotencia, liberar las vencidas, ver si está ocupado, retener y cobrar.

**63.** PaymentResult necesita el ID, el resultado de la transacción y el usuario. Es solo para informar el resultado del pago.

**64.** ¿IPaymentServiceClient es el contrato de cómo se comunica Booking con Payment? Vamos con PaymentServiceClient: ¿no debería conseguir primero el token?

**65.** Si el worker recibe el ID como número y Booking lo espera como texto, no lo reconocería.

**66.** Anoté que ReservationRequestedMessage es el molde del JSON que llega al worker, que lo llena JsonSerializer y que para EventId y SeatId usa el FlexibleStringJsonConverter. ¿Está bien?

**67.** A Booking no le importan los asientos ni los eventos, solo las solicitudes: por eso no necesita esas tablas.

## Lectura de las guías y repaso del flujo

**68.** Si el evento y el asiento tienen el mismo RoomId, el asiento es de la sala de ese evento. Que la capacidad sea 10 y haya 10 asientos está bien.

**69.** El Number es el número de siempre y el Id es el de la base.

**70.** Puede crear la solicitud de forma asincrónica porque el usuario ve "recibida" aunque Booking todavía no la tomó. Si no, las flechas irían para afuera, y en Clean eso no puede pasar.

**71.** ¿De qué tipo es el request que recibe AddAsync en ReservationRequestRepository?

**72.** El caso de uso usa IReservationRequestRepository porque tiene un atributo de ese tipo, y lo implementa ReservationRequestRepository. Siento que hay un problema de nombres: no veo ninguna clase ReservationRequest.

**73.** ¿Por qué el caso de uso crea una ReservationRequest si no hay ninguna flecha?

**74.** Saqué GetEventos y GetDisponibilidad de ReservationsController. ¿Qué es EventsController? Es el de otro caso de uso, ¿no?

**75.** ¿ReservationRequestRepository solo guarda las solicitudes o también las consulta?

**76.** Traduce para que el caso de uso no dependa de lo que llega crudo de la pantalla. Si cambia lo que se muestra, se toca solo el ViewModel.

**77.** Repaso el flujo: del controller azul todo entra al verde, que llama a solicitar reserva; el caso de uso llama al output port, y como Program lo conectó, se ejecuta el presenter, ¿no?

**78.** En la guía dice que el caso de uso hace todos los pasos de solicitar reserva. ¿Hay algo especial que tenga que saber de las clases chiquitas, la que entra, la que sale y el mensaje que viaja a Booking?

**79.** ReservationRequested es lo que viaja a Booking y RequestReservationResult es lo que ve el usuario.

**80.** Las herramientas hacen lo que el caso de uso les pide en Execute. ¿ReservationRequested lo crea o lo usa? ¿Dónde guarda el repositorio y adónde manda el publisher? Aunque use la interfaz, se ejecuta la clase concreta que conectó Program.

**81.** Availability no usa BookingService directo, por eso no lo ponemos.

**82.** Leyendo Booking, no entiendo cómo leer el cuadro de Keycloak, "lo que llega y lo que sale".

**83.** ¿Qué es una fábrica de scopes y por qué el worker no usa IBookingService directo?

**84.** Quiero entender cómo se mueven las cosas por el piso rojo: las clases chicas (PaymentResult, ReservationRequestedMessage y el converter) me confunden.

**85.** En la guía dice que ReservationsController no decide nada del negocio y que ver eventos y asientos no requiere token: ¿dónde está eso? Ah, es otro caso de uso.

**86.** Te explico el flujo: ReservaController recibe del controller azul, traduce y llama a Execute; al terminar, el caso de uso llama a Presentar y se arma el ViewModel. Presentar sabe armar el ViewModel porque lo tiene como atributo. El Result es lo que se le pasa al presenter, ¿no?

## El recorrido en voz alta y repaso final

**87.** Booking no tiene un método ProcessPaymentAsync propio: ¿en algún momento BookingService lo llama?

**88.** Te cuento el recorrido de una reserva en Availability, caja por caja, y me corregís lo que digo mal.

**89.** Ahora el de Booking, que estoy medio perdido. ¿Dónde usa el worker a IBookingService?

**90.** ¿Qué hace ReservationRequestedMessage y quién más lo usa? Todavía no se consume nada: BookingService va a usar esos datos para los chequeos. Y al final, lo que vuelve de Payment sería un PaymentResult, ¿no?

**91.** En IBookingService, ¿no debería ser BookingService el que se conecte con la entidad, que es quien la crea? La interfaz solo declara los métodos.

**92.** ¿En qué parte devuelve un BookingEntity? ¿Cómo se leen esas líneas de la interfaz?

**93.** ¿Qué es el worker de reservas?

**94.** No entiendo cuál es el problema con Program en los dos diagramas. Ah, esos métodos no existen en el código: solo agrupan las líneas de Program.
