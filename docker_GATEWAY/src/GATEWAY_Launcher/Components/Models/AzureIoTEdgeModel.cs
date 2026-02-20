namespace GATEWAY_Launcher.Components.Models
{

    public class AzureIoTEdgeModel
    {
        public Dictionary<string, string> StatusMap { get; set; } = new();
        public List<ModuleInfo> Moduledata { get; set; } = new();
    }


    public class ModuleInfo
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Config { get; set; }
    }

    public class ConnectionSettingsModel
    {
        public bool IsX509 { get; set; }
        public string? ConnectionString { get; set; }
        public string? Host { get; set; }
        public string? DeviceID { get; set; }
        public X509Identity Identity { get; set; } = new();
    }

    public class X509Identity
    {
        public string? CertPath { get; set; }
        public string? KeyPath { get; set; }
    }
}