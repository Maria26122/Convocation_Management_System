using Convocation_Management_System.Web.UI.Helpers;

namespace Convocation_Management_System.Web.UI.Helpers
{
    public static class SimpleQrBuilder
    {
        public static string Build(int registrationId, int eventId, int userId)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return $"REG:{registrationId}|EVT:{eventId}|UID:{userId}|TS:{timestamp}";
        }
    }
}