using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

namespace GATEWAY_Launcher.Components.Models
{
    public class TimeSettingModel
    {
        public DateTime CurrentTime { get; set; }
        public string? SelectedTimeZone { get; set; }
        public List<string?> TimeZones { get; set; } = new();
        public NtpSettingModel NtpSetting { get; set; } = new();
    }

    public class EditDateModel
    {
        public string Year { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Day { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Minute { get; set; } = string.Empty;
    }

    public class NtpSettingModel
    {
        public bool SyncCheck { get; set; }
        public string NtpServer { get; set; } = string.Empty;
    }
}
