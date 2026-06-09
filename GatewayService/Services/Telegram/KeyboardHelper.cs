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
                    keyboardButtonsLine2.Add(new KeyboardButton("⚙️ Сменить статус бронирования"));
                    keyboardButtonsLine3.Add(new KeyboardButton("⚙️ Сменить статус комнаты"));
                    List<KeyboardButton> keyboardButtonsLine4 = new List<KeyboardButton>
                    {
                        new KeyboardButton("⚙️ Показать все бронирования"),
                    };
                    buttons.Add(keyboardButtonsLine1);
                    buttons.Add(keyboardButtonsLine2);
                    buttons.Add(keyboardButtonsLine3);
                    buttons.Add(keyboardButtonsLine4);
                }
                else
                {
                    buttons.Add(keyboardButtonsLine1);
                    buttons.Add(keyboardButtonsLine2);
                    buttons.Add(keyboardButtonsLine3);
                }
                
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
                .Where(r => r.Status == RoomStatus.Available)
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

        public static InlineKeyboardMarkup GetBookingSelectionKeyboard(
            List<BookingResponse> bookings,
            Dictionary<Guid, string> userNames,
            Dictionary<int, string> roomNames)
        {
            var buttons = bookings
                .Select(b =>
                {
                    var userName = userNames.TryGetValue(b.UserId, out var u) ? u : "Неизвестный";
                    var roomName = roomNames.TryGetValue(b.RoomId, out var r) ? r : "Неизвестно";
                    var label = $"{roomName} — {userName} {b.Period?.TimeBegin?.ToLocalTime():dd.MM HH:mm}";
                    return new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(label, $"booking_{b.Id}")
                    };
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
                    InlineKeyboardButton.WithCallbackData(status.ToString(), $"status_{status}")
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

        public static InlineKeyboardMarkup GetRoomStatusSelectionKeyboard(List<RoomResponse> rooms)
        {
            var buttons = rooms
                .Select(r => new List<InlineKeyboardButton>
                {
            InlineKeyboardButton.WithCallbackData(
                $"{r.Name} — {(r.Status == RoomStatus.Available ? "✅" : "🔧")}",
                $"roomstatus_{r.Id}")
                })
                .ToList();

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetRoomNewStatusKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Доступна",     "newroomstatus_Available") },
                new[] { InlineKeyboardButton.WithCallbackData("🔧 Недоступна",   "newroomstatus_Maintenance") },
                new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена",       "cancel") }
            });
        }
    }
}