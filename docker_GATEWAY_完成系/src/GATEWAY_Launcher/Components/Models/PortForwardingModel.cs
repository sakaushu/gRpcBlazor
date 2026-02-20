namespace GATEWAY_Launcher.Components.Models
{
    public class PortForwardingModel
    {
        public List<PortForwardingElement> Elements { get; set; } = new();
    }

    public class PortForwardingElement
    {
        public string? Interface { get; set; }
        public bool IsTcp { get; set; }
        public int? ReceptionPort { get; set; }
        public string? DestinationIp { get; set; }
        public int? DestinationPort { get; set; }
    }
}