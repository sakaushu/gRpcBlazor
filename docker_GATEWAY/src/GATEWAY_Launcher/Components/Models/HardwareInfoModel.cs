namespace GATEWAY_Launcher.Components.Models
{
    public class HardwareInfoModel
    {
        public string? CpuInfo { get; set; }
        public double? CpuUsage { get; set; }
        public double? CpuTemperature { get; set; }
        public double? MemoryUsage { get; set; }
        public double? MemoryAvailable { get; set; }
        public double? DiskUsage { get; set; }
        public double? DiskAvailable { get; set; }
    }
}