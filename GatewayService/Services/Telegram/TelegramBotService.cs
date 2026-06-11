using GatewayService.Configuration;
using GatewayService.Handlers;
using GatewayService.Middleware;
using GatewayService.Models.Conversation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GatewayService.Services.Telegram
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessageSender _messageSender;
        private readonly ITelegramBotClient _botClient;

        public TelegramBotService(IOptions<TelegramSettings> telegramSettings, ILogger<TelegramBotService> logger, IMessageSender messageSender, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _messageSender = messageSender;
            _scopeFactory = scopeFactory;
            _botClient = new TelegramBotClient(telegramSettings.Value.BotToken);
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update == null)
                return;

            using var scope = _scopeFactory.CreateScope();

            try
            {
                if (update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(update.CallbackQuery, scope);
                    return;
                }

                if (update.Message?.Text != null)
                {
                    await HandleMessageAsync(update.Message, scope);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, IServiceScope scope)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            var data = callbackQuery.Data!;

            _logger.LogInformation("Received callback from ChatId: {ChatId}, Data: {Data}", chatId, data);

            await _botClient.AnswerCallbackQuery(callbackQuery.Id);

            if (data.StartsWith("menu_"))
            {
                var username = callbackQuery.From?.Username ?? callbackQuery.From?.Id.ToString() ?? chatId.ToString();
                await HandleMenuCallbackWithScope(chatId, data, scope, username);
            }
            else if (data.StartsWith("room_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleRoomSelectionCallback(chatId, data);
            }
            else if (data.StartsWith("booking_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleBookingSelectionCallback(chatId, data);
            }
            else if (data.StartsWith("cancelbook_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleCancelBookingSelectionCallback(chatId, data);
            }
            else if (data.StartsWith("confirmcancel_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleConfirmCancelBookingCallback(chatId, data);
            }
            else if (data.StartsWith("status_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleBookingStatusSelectionCallback(chatId, data);
            }
            else if (data.StartsWith("roomstatus_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                await handler.HandleRoomStatusSelectionCallback(chatId, data);
            }
            else if (data.StartsWith("newroomstatus_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                await handler.HandleRoomNewStatusCallback(chatId, data);
            }
            else if (data == "cancel")
            {
                var stateService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();
                stateService.Clear(chatId);
                await _messageSender.SendMessageAsync(chatId, "❌ Действие отменено.");
            }
        }

        private async Task HandleMessageAsync(Message message, IServiceScope scope)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text!;

            _logger.LogInformation("Received message from ChatId: {ChatId}, Text: {Text}", chatId, messageText);

            var stateService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();
            var conversation = stateService.GetOrCreate(chatId);

            switch (messageText)
            {
                case "/start":
                case "🏠 Главное меню":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                        await handler.HandleStartCommand(chatId);
                        break;
                    }

                case "/help":
                case "ℹ️ Помощь":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                        await handler.HandleHelpCommand(chatId);
                        break;
                    }

                case "📝 Регистрация":
                    {
                        conversation.Clear();
                        conversation.SetValue("username", message.From?.Username ?? message.From?.Id.ToString() ?? chatId.ToString());
                        conversation.State = ConversationState.AwaitingRegistrationFirstName;
                        await _messageSender.SendMessageAsync(chatId,
                            "📝 Регистрация\n\nВведите Имя:",
                            replyMarkup: KeyboardHelper.GetLoginPasswordKeyboard());
                        break;
                    }

                case "🔑 Войти":
                    {
                        conversation.Clear();
                        var authMiddleware = scope.ServiceProvider.GetRequiredService<TelegramAuthMiddleware>();
                        var isAuthenticated = await authMiddleware.EnsureAuthenticatedAsync(chatId);
                        if (isAuthenticated)
                        {
                            await _messageSender.SendMessageAsync(chatId, "✅ Вход выполнен успешно!");
                            var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                            await handler.HandleStartCommand(chatId);
                        }
                        else
                        {
                            await _messageSender.SendMessageAsync(chatId,
                                "❌ Не удалось выполнить вход.\n\n" +
                                "Вы еще не зарегистрированы. Используйте кнопку '📝 Регистрация'.");
                        }
                        break;
                    }

                case "/myprofile":
                case "👤 Мой профиль":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                        await handler.HandleMyProfileCommand(chatId);
                        break;
                    }

                case "✏️ Изменить профиль":
                    {
                        conversation.Clear();
                        conversation.State = ConversationState.AwaitingUpdateProfileField;
                        await _messageSender.SendMessageAsync(chatId,
                            "✏️ Обновление профиля\n\nВведите данные в формате:\nusername firstname lastname\n\nИспользуйте '-' для пропуска.");
                        break;
                    }

                case "/rooms":
                case "🏠 Комнаты":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                        await handler.HandleGetRoomsCommand(chatId);
                        break;
                    }

                case "/bookings":
                case "📋 Мои бронирования":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await handler.HandleGetMyBookingsCommand(chatId);
                        break;
                    }

                case "📅 Забронировать комнату":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await handler.StartCreateBooking(chatId, conversation);
                        break;
                    }

                case "❌ Отменить бронирование":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await handler.HandleCancelMyBookingInput(chatId, conversation);
                        break;
                    }

                case "⚙️ Показать все бронирования":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await handler.HandleGetBookingsCommand(chatId);
                        break;
                    }

                case "⚙️ Сменить статус бронирования":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await handler.HandleUpdateBookingInput(chatId, conversation);
                        break;
                    }

                case "⚙️ Сменить статус комнаты":
                    {
                        conversation.Clear();
                        var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                        await handler.HandleUpdateRoomStatusInput(chatId, conversation);
                        break;
                    }

                default:
                    {
                        if (conversation.State != ConversationState.None)
                        {
                            if (messageText == "❌ Отмена")
                            {
                                conversation.Clear();
                                await _messageSender.SendMessageAsync(chatId, "❌ Действие отменено.");
                                var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                                await handler.HandleStartCommand(chatId);
                                return;
                            }

                            await HandleConversationMessageAsync(chatId, messageText, conversation, scope);
                            return;
                        }

                        await _messageSender.SendMessageAsync(chatId, "Неизвестная команда. Используйте меню.");
                        break;
                    }
            }
        }

        private async Task HandleConversationMessageAsync(long chatId, string messageText, UserConversationData conversation, IServiceScope scope)
        {
            switch (conversation.State)
            {
                case ConversationState.AwaitingRegistrationFirstName:
                    var regHandler2 = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await regHandler2.HandleRegistrationFirstNameInput(chatId, messageText, conversation);
                    break;

                case ConversationState.AwaitingRegistrationLastName:
                    var regHandler3 = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await regHandler3.HandleRegistrationLastNameInput(chatId, messageText, conversation);
                    break;

                case ConversationState.AwaitingUpdateProfileField:
                    var updateHandler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await updateHandler.HandleUpdateProfileInput(chatId, messageText, conversation);
                    break;

                case ConversationState.AwaitingBookingDate:
                    var bookingHandler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await bookingHandler.HandleBookingDateInput(chatId, messageText, conversation);
                    break;

                case ConversationState.AwaitingBookingStartTime:
                    var bookingHandler2 = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await bookingHandler2.HandleBookingStartTimeInput(chatId, messageText, conversation);
                    break;

                case ConversationState.AwaitingBookingEndTime:
                    var bookingHandler3 = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await bookingHandler3.HandleBookingEndTimeInput(chatId, messageText, conversation);
                    break;

                default:
                    await _messageSender.SendMessageAsync(chatId, "Произошла ошибка. Попробуйте начать заново.");
                    break;
            }
        }

        private async Task HandleMenuCallbackWithScope(long chatId, string callbackData, IServiceScope scope, string telegramUsername)
        {
            var stateService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();
            var conversation = stateService.GetOrCreate(chatId);
            conversation.Clear();

            // Команды, не требующие авторизации
            if (callbackData == "menu_register")
            {
                conversation.SetValue("username", telegramUsername);
                conversation.State = ConversationState.AwaitingRegistrationFirstName;
                await _messageSender.SendMessageAsync(chatId,
                    "📝 Регистрация\n\nВведите Имя:",
                    replyMarkup: KeyboardHelper.GetCancelKeyboard());
                return;
            }

            // Все остальные команды требуют авторизации
            var authMiddleware = scope.ServiceProvider.GetRequiredService<TelegramAuthMiddleware>();
            var isAuthenticated = await authMiddleware.EnsureAuthenticatedAsync(chatId);

            if (!isAuthenticated)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "⚠️ Требуется авторизация. Используйте /start для регистрации.");
                return;
            }

            switch (callbackData)
            {
                case "menu_register":
                    conversation.State = ConversationState.AwaitingRegistrationFirstName;
                    await _messageSender.SendMessageAsync(chatId,
                        "📝 Регистрация\n\nВведите Имя:",
                        replyMarkup: KeyboardHelper.GetCancelKeyboard());
                    break;

                case "menu_myprofile":
                    var identityHandler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await identityHandler.HandleMyProfileCommand(chatId);
                    break;

                case "menu_update_profile":
                    conversation.State = ConversationState.AwaitingUpdateProfileField;
                    await _messageSender.SendMessageAsync(chatId,
                        "✏️ Обновление профиля\n\nВведите данные в формате:\nusername firstname lastname\n\nИспользуйте '-' для пропуска.",
                        replyMarkup: KeyboardHelper.GetCancelKeyboard());
                    break;

                case "menu_rooms":
                    var roomHandler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await roomHandler.HandleGetRoomsCommand(chatId);
                    break;

                case "menu_create_booking":
                    var bookingHandler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await bookingHandler.StartCreateBooking(chatId, conversation);
                    break;

                case "menu_get_bookings":
                    var bookingHandler2 = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await bookingHandler2.HandleGetBookingsCommand(chatId);
                    break;
            }
        }
    }
}
