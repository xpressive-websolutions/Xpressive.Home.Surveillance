using System;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance
{
    public class RemoteDeviceWithValidation : RemoteDevice
    {
        public bool IsValidNonce { get; set; } = true;
        public string LastSeen => GetLastSeen();

        private string GetLastSeen()
        {
            var time = DateTime.UtcNow - LastResponse;

            if (time.TotalMinutes < 1) return $"{time.TotalSeconds:N0} seconds ago";
            if (time.TotalHours < 1) return $"{time.TotalMinutes:N0} minutes ago";
            if (time.TotalDays < 2) return $"{time.TotalHours:N0} hours ago";
            return $"{time.TotalDays:N1} days ago";
        }
    }
}
