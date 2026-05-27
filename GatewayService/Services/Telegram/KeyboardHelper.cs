using Telegram.Bot.Types.ReplyMarkups;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;

namespace GatewayService.Services.Telegram
{
    public static class KeyboardHelper
    {
        public static ReplyKeyboardMarkup GetMainMenu(bool isAuthenticated, string? userRole = null)
        {
            var buttons = new List<List<KeyboardButton>>();

            if (!isAuthenticated)
            {
                buttons.Add(new List<KeyboardButton>
                {
                    new KeyboardButton("📝 Регистрация"),
                    new KeyboardButton("🔑 Войти")
                });
            }
            else
            {
                List<KeyboardButton> keyboardButtonsLine1 = new List<KeyboardButton>
                {
                    new KeyboardButton("👤 Мой профиль"),
                    new KeyboardButton("✏️ Изменить профиль")
                };

                List<KeyboardButton> keyboardButtonsLine2 = new List<KeyboardButton>
                {
                    new KeyboardButton("📋 Мои бронирования"),
                    new KeyboardButton("📅 Забронировать комнату"),
                };

                List<KeyboardButton> keyboardButtonsLine3 = new List<KeyboardButton>
                {
                    new KeyboardButton("🏠 Комнаты"),
                };

                if (userRole == "Moderator" || userRole == "Administrator")
                {
                    keyboardButtonsLine2.Add(new KeyboardButton("Сменить статус бронирования"));
                    keyboardButtonsLine3.Add(new KeyboardButton("Сменить статус комнаты"));
                }

                buttons.Add(keyboardButtonsLine1);
                buttons.Add(keyboardButtonsLine2);
                buttons.Add(keyboardButtonsLine3);
            }

            return new ReplyKeyboardMarkup(buttons)
            {
                ResizeKeyboard = true, // Подгоняет размер кнопок
                OneTimeKeyboard = false // Кнопки всегда видны
            };
        }

        public static InlineKeyboardMarkup GetRoomSelectionKeyboard(List<RoomResponse> rooms)
        {
            var buttons = rooms
                .Where(r => r.Status == 0)
                .Select(r => new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"🏠 {r.Name}", $"room_{r.Id}")
                })
                .ToList();

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetBookingSelectionKeyboard(List<BookingResponse> bookings)
        {
            var buttons = bookings
                .Where(b => b.Status == BookingStatus.Booked || b.Status == BookingStatus.NotConfirmed)
                .Select(b => new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"Пользователь: {b.UserId} Комната: {b.RoomId} Время: {b.TimeBegin} - {b.TimeEnd}", $"booking_{b.Id}")
                })
                .ToList();
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetBookingStatusSelectionKeyboard()
        {
            List<List<InlineKeyboardButton>>? buttons = new();

            foreach (BookingStatus status in Enum.GetValues(typeof(BookingStatus)))
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(status.ToString(), status.ToString())
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetCancelKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") }
            });
        }

        public static ReplyKeyboardMarkup GetLoginPasswordKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { new KeyboardButton("❌ Отмена") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true // Скрывается после нажатия
            };
        }
    }
}