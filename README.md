## OTUS проект "Телеграм бот музыкальной студии"

### Основной функционал:
* Управление Бронированием Кабинетов;
* Управление Профилем Пользователя;
* Управление Ресурсами Студии (Кабинетами);
* Управление Бронированиями Студии (для Администраторов);
* Система Уведомлений.

<img width="856" height="875" alt="musicstudiobot" src="https://github.com/user-attachments/assets/79f24e4b-25dd-4790-93a7-2bbac68ac441" />

## Описание архитектуры MusicStudioBot

Система реализована в виде набора микросервисов на платформе **.NET / ASP.NET Core**, развёртываемых через **Docker Compose**. Межсервисное взаимодействие строится на двух механизмах: синхронные HTTP-вызовы (REST) и асинхронный обмен сообщениями через **RabbitMQ**.

---

### GatewayService

Единственная точка входа для всех пользователей, взаимодействующих через **Telegram**. Бот работает в режиме polling с использованием библиотеки `Telegram.Bot`. Диалоговое состояние управляется через `ConversationState` enum — кнопки главного меню (`ReplyKeyboardMarkup`) обрабатываются до проверки состояния, чтобы нажатие кнопки не попало в обработчик диалога.

Аутентификация реализована через `TelegramAuthMiddleware` по трёхступенчатой схеме: проверка access token → попытка refresh → автологин по `TelegramId`. Токены (access + refresh) хранятся в **Redis** (`StackExchange.Redis`) под ключом `telegram_session:{telegramId}` с TTL 30 дней. Для формирования ответов пользователю сервис выполняет API Composition: параллельно запрашивает имена комнат и пользователей из смежных сервисов, чтобы избежать N+1 HTTP-запросов.

---

### IdentityService

Отвечает за регистрацию, аутентификацию и управление пользователями. JWT-токены подписываются алгоритмом **HMAC-SHA256**, срок действия — 60 минут. Refresh-токены хранятся в PostgreSQL, срок действия — 30 дней. При обращении через GatewayService аутентификация выполняется по `TelegramId` (`POST /api/auth/user/telegram/login`). Для AdminPanel предусмотрен отдельный endpoint `POST /api/auth/admin/login` без перебора пользователей. Пароли генерируются сервером при регистрации; изменение `Username` после регистрации недоступно.

---

### BookingService

Управляет жизненным циклом бронирований. Все операции создания и обновления проходят через `BookingValidationPipeline` (проверка пересечений, будущей даты и т.д.); операции смены статуса администратором пайплайн обходят. После каждой write-операции сервис публикует доменные события в **RabbitMQ** через `IMessageDispatcher`:

- в очередь `logging_service_queue` — событие `BookingEvent` с уровнем, типом и текстом сообщения;
- в очередь `statistic_service_queue` — событие `StatisticEvent` с типом операции (`CreatedBooking`, `DeletedBooking`, `UpdatedBooking`).

Сообщения публикуются с флагом `Persistent = true`. Очереди объявляются при старте сервиса, что исключает ошибку `NO_ROUTE` при отсутствии консьюмеров.

---

### RoomService

Управляет комнатами и категориями. Реализует OutboundMessages-паттерн для публикации событий в RabbitMQ (в процессе интеграции).

---

### RabbitMQ

Брокер сообщений (`rabbitmq:3-management`). Используется исключительно как транспорт для write-событий — логирования и сбора статистики. Default exchange, прямая маршрутизация по имени очереди. Порт управления — `15672`.

---

### LoggingService

`BackgroundService`, подписанный на очередь `logging_service_queue`. Десериализует входящие `LogEventDto` и сохраняет записи в PostgreSQL. Реализован на базе **Serilog**. Ошибки публикации в RabbitMQ изолированы через `try-catch` внутри `ILoggingService` и не прерывают основной бизнес-поток.

---

### StatisticService

`BackgroundService`, подписанный на очередь `statistic_service_queue`. При получении события инкрементирует счётчики в **Redis** через `StackExchange.Redis`:

- `HINCRBY stats:bookings:daily <дата> 1`
- `HINCRBY stats:deletebookings:daily <дата> 1`
- `HINCRBY stats:updatebookings:daily <дата> 1`

Дополнительно ведёт строковые ключи вида `stats:bookings:yyyy-MM-dd` для точечного запроса по дате.

---

### Redis

`redis:7-alpine`, порт `6379`. Выполняет две роли: хранилище сессий GatewayService (строки с TTL) и хранилище метрик StatisticService (Redis Hash).

---

### Grafana

`grafana/grafana:latest`, порт `3001`. Подключается к Redis через плагин `redis-datasource` (устанавливается через `GF_INSTALL_PLUGINS`). Дашборд содержит три панели типа `bargauge`, каждая выполняет `HGETALL` по соответствующему ключу статистики.

---

### AdminPanel

Одностраничное HTML/JS-приложение, работающее в браузере. Обращается напрямую к IdentityService, BookingService и RoomService по HTTP с JWT в заголовке `Authorization: Bearer`. При получении `401` автоматически выполняет refresh токена. Содержит вкладки управления пользователями, комнатами/категориями и бронированиями с сортировкой по столбцам и inline-редактированием статусов.

---

### PostgreSQL

Один инстанс `postgres:16`, порт `5432`, база `music_studio_bot`. Используется четырьмя сервисами с логическим разделением по префиксам таблиц: `identity_*`, `booking_*`, `room_*`, `logging_*`. Миграции управляются через **Entity Framework Core**.

