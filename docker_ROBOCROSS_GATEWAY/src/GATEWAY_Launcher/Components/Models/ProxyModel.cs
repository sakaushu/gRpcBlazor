using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GATEWAY_Launcher.Components.Models
{
    public class ProxyModel
    {
        public bool SelectedProxyType { get; set; }
        public string? ProxyServer { get; set; }
        public int? ProxyPort { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}