using System;
using System.Linq;
using System.Net;

namespace Mtf.Wmi.Services
{
    public static class LocalDeviceIdentifier
    {
        public static bool IsLocalMachine(string computerNameOrIp)
        {
            if (String.IsNullOrWhiteSpace(computerNameOrIp) || computerNameOrIp == "." || computerNameOrIp == Constants.Localhost)
            {
                return true;
            }

            if (IPAddress.TryParse(computerNameOrIp, out var ipAddress) && IPAddress.IsLoopback(ipAddress))
            {
                return true;
            }

            var hostName = Dns.GetHostName();
            var localIPs = Dns.GetHostAddresses(hostName)
                .Select(ip => ip.ToString())
                .ToList();

            return localIPs.Contains(computerNameOrIp, StringComparer.OrdinalIgnoreCase);
        }
    }
}
