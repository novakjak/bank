using System.Net;

public static class BankCodeUtil
{
    public static string Normalize(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        return ip.ToString();
    }

    public static string Normalize(string ip)
        => Normalize(IPAddress.Parse(ip));
}
