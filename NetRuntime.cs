using System;
using System.Net;

internal static class NetRuntime
{
    private static bool _initialized;
    private static readonly object _gate = new object();

    public static void Ensure()
    {
        if (_initialized)
            return;

        lock (_gate)
        {
            if (_initialized)
                return;

            ServicePointManager.DefaultConnectionLimit = Math.Max(ServicePointManager.DefaultConnectionLimit, 50);
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;

            // .NET Framework 4.7.2: güvenli protokoller
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            _initialized = true;
        }
    }
}
