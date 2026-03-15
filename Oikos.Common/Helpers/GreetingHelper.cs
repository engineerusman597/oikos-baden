using Microsoft.Extensions.Localization;
using Oikos.Common.Resources;

namespace Oikos.Common.Helpers;

public static class GreetingHelper
{
    public static string BuildGreeting(string? userName, IStringLocalizer<SharedResource> localizer, int? clientHour = null)
    {
        var hour = clientHour ?? DateTime.Now.Hour;
        var periodKey = hour switch
        {
            >= 5 and < 12 => "Morning",
            >= 12 and < 18 => "Day",
            _ => "Evening",
        };

        if (string.IsNullOrWhiteSpace(userName))
        {
            return localizer[$"HomePage_Greeting{periodKey}WithoutName"].Value;
        }

        return string.Format(localizer[$"HomePage_Greeting{periodKey}WithName"].Value, userName);
    }
}
