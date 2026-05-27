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
                await HandleMenuCallbackWithScope(chatId, data, scope);
            }
            else if (data.StartsWith("room_"))
            {
                var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                await handler.HandleRoomSelectionCallback(chatId, data);
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
            switch (messageText)
            {
                case "/start":
                case "🏠 Главное меню":
                    {
                        var startHandler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                        await startHandler.HandleStartCommand(chatId);
                        break;
                    }

                case "/help":
                case "ℹ️ Помощь":
                    {
                        var helpHandler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                        await helpHandler.HandleHelpCommand(chatId);
                        break;
                    }
                case "📝 Регистрация":
                    {
                        conversation.State = ConversationState.AwaitingRegistrationPassword;
                        await _messageSender.SendMessageAsync(chatId,
                        "📝 Регистрация\n\nВведите пароль:",
                        replyMarkup: KeyboardHelper.GetLoginPasswordKeyboard());
                        break;
                    }

                case "🔑 Войти":
                    {
                        var authMiddleware = scope.ServiceProvider.GetRequiredService<TelegramAuthMiddleware>();
                        var isAuthenticated = await authMiddleware.EnsureAuthenticatedAsync(chatId);

                        if (isAuthenticated)
                        {
                            await _messageSender.SendMessageAsync(chatId,
                                "✅ Вход выполнен успешно!");

                            var startHandler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                            await startHandler.HandleStartCommand(chatId);
                        }
                        else
                        {
                            await _messageSender.SendMessageAsync(chatId,
                                "❌ Не удалось выполнить вход.\n\n" +
                                "Вы еще не зарегистрированы. Используйте кнопку '📝 Регистрация'.");
                        }
                    
                        break; 
                    }

                default:
                    {
                        var authMiddleware = scope.ServiceProvider.GetRequiredService<TelegramAuthMiddleware>();
                        var isAuthenticated = await authMiddleware.EnsureAuthenticatedAsync(chatId);

                        if (!isAuthenticated)
                        {
                            await _messageSender.SendMessageAsync(chatId,
                                "⚠️ Требуется авторизация.");
                            return;
                        }

                        await HandleAuthenticatedMenuAsync(chatId, messageText, scope);
                        break;
                    }
            }
        }

        private async Task HandleAuthenticatedMenuAsync(long chatId, string messageText, IServiceScope scope)
        {
            switch (messageText)
            {
                case "/myprofile":
                case "👤 Мой профиль":
                    {
                        var profileHandler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                        await profileHandler.HandleMyProfileCommand(chatId);
                        break;
                    }

                case "✏️ Изменить профиль":
                    {
                        var stateService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();
                        var conversation = stateService.GetOrCreate(chatId);
                        conversation.State = ConversationState.AwaitingUpdateProfileField;
                        await _messageSender.SendMessageAsync(chatId,
                        "✏️ Обновление профиля\n\nВведите данные в формате:\nusername firstname lastname\n\nИспользуйте '-' для пропуска.");
                        break;
                    }
                case "/rooms":
                case "🏠 Комнаты":
                    {
                        var roomHandler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                        await roomHandler.HandleGetRoomsCommand(chatId);
                        break;
                    }

                case "📅 Забронировать комнату":
                    {
                        var bookingHandler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        var conv = scope.ServiceProvider.GetRequiredService<IConversationStateService>().GetOrCreate(chatId);
                        await bookingHandler.StartCreateBooking(chatId, conv);
                            break;
                    }
                case "/bookings":
                case "📋 Мои бронирования":
                    {
                        var bookingsHandler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                        await bookingsHandler.HandleGetBookingsCommand(chatId);
                        break;
                    }

                //case "Сменить статус бронирования":
                //   { 
                //        var bookingStatusHandler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                //        var conv = scope.ServiceProvider.GetRequiredService<IConversationStateService>().GetOrCreate(chatId);
                //        await bookingStatusHandler.HandleUpdateBookingInput(chatId, conv);
                //        break;
                //    }

                default:
                    await _messageSender.SendMessageAsync(chatId,
                        "Неизвестная команда. Используйте меню.");
                    break;
            }
        }

        private async Task HandleConversationMessageAsync(long chatId, string messageText, UserConversationData conversation, IServiceScope scope)
        {
            switch (conversation.State)
            {
                case ConversationState.AwaitingRegistrationPassword:
                    var regHandler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await regHandler.HandleRegistrationPasswordInput(chatId, messageText, conversation);
                    break;

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

        private async Task HandleMenuCallbackWithScope(long chatId, string callbackData, IServiceScope scope)
        {
            var stateService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();
            var conversation = stateService.GetOrCreate(chatId);
            conversation.Clear();

            // Команды, НЕ требующие авторизации
            if (callbackData == "menu_register")
            {
                conversation.State = ConversationState.AwaitingRegistrationPassword;
                await _messageSender.SendMessageAsync(chatId,
                    "📝 Регистрация\n\nВведите пароль:",
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
                    conversation.State = ConversationState.AwaitingRegistrationPassword;
                    await _messageSender.SendMessageAsync(chatId,
                        "📝 Регистрация\n\nВведите пароль:",
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
