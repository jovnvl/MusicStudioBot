namespace BookingService.Common
{
    static class Constants
    {
        public const string LOGIN_SERVICE_QUEUE = @"logging_service_queue";
        public const string STATISTIC_SERVICE_QUEUE = @"statistic_service_queue";
        public const string NOTIFICATION_SERVICE_QUEUE = @"notification_service_queue";

        public const string Logs = "logs";
        public const string Statistics = "statistics";
        public const string Notifications = "notifications";

        public const string BookingCreated = "booking-created";
        public const string BookingDeleted = "booking-deleted";
        public const string BookingUpdated = "booking-updated";
    }
}
