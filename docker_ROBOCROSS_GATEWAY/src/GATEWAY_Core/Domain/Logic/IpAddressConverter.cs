namespace GATEWAYCore.Domain.Logic;

/// <summary>
/// IPアドレスと数値の相互変換ロジック
/// </summary>
public static class IpAddressConverter
{
    /// <summary>
    /// プレフィックス長をサブネットマスクのuint値に変換 (big-endian)
    /// 例：24 → 0xFFFFFF00
    /// </summary>
    public static uint PrefixToUint32Mask(int prefix)
    {
        if (prefix < 0 || prefix > 32)
        {
            return 0xFFFFFF00; // デフォルトは /24
        }

        return prefix == 0 ? 0 : ~0u << (32 - prefix);
    }

    /// <summary>
    /// uint を IP アドレス文字列に変換（little-endian）
    /// 例：0x0101A8C0 → "192.168.1.1"
    /// </summary>
    public static string Uint32ToIpv4LittleEndian(uint v)
    {
        var b0 = (byte)(v & 0xFF);
        var b1 = (byte)((v >> 8) & 0xFF);
        var b2 = (byte)((v >> 16) & 0xFF);
        var b3 = (byte)((v >> 24) & 0xFF);
        return $"{b0}.{b1}.{b2}.{b3}";
    }

    /// <summary>
    /// uint を IP アドレス文字列に変換（big-endian）
    /// 例：0xC0A80101 → "192.168.1.1"
    /// </summary>
    public static string Uint32ToIpv4BigEndian(uint v)
    {
        var b0 = (byte)((v >> 24) & 0xFF);
        var b1 = (byte)((v >> 16) & 0xFF);
        var b2 = (byte)((v >> 8) & 0xFF);
        var b3 = (byte)(v & 0xFF);
        return $"{b0}.{b1}.{b2}.{b3}";
    }

    /// <summary>
    /// IP アドレス文字列を uint に変換（little-endian）
    /// 例："192.168.1.1" → 0x0101A8C0
    /// </summary>
    public static uint Ipv4ToUint32LittleEndian(string ipv4)
    {
        var parts = ValidateAndSplitIpAddress(ipv4);

        uint result = 0;
        for (int i = 0; i < 4; i++)
        {
            result |= parts[i] << (i * 8);
        }
        return result;
    }

    /// <summary>
    /// IP アドレス文字列を uint に変換（big-endian）
    /// 例："192.168.1.1" → 0xC0A80101
    /// </summary>
    public static uint Ipv4ToUint32BigEndian(string ipv4)
    {
        var parts = ValidateAndSplitIpAddress(ipv4);

        uint result = 0;
        for (int i = 0; i < 4; i++)
        {
            result |= parts[i] << ((3 - i) * 8);
        }
        return result;
    }

    /// <summary>
    /// IPアドレス文字列を検証し、バイト配列に分割する
    /// </summary>
    /// <param name="ipv4">IPアドレス文字列</param>
    /// <returns>4つのバイト値の配列</returns>
    /// <exception cref="ArgumentException">無効なIPアドレスの場合</exception>
    private static uint[] ValidateAndSplitIpAddress(string ipv4)
    {
        if (string.IsNullOrEmpty(ipv4))
        {
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipv4));
        }

        var parts = ipv4.Split('.');
        if (parts.Length != 4)
        {
            throw new ArgumentException($"Invalid IPv4 address: {ipv4}");
        }

        var bytes = new uint[4];
        for (int i = 0; i < 4; i++)
        {
            if (!uint.TryParse(parts[i], out var part) || part > 255)
            {
                throw new ArgumentException($"Invalid IPv4 address: {ipv4}");
            }
            bytes[i] = part;
        }

        return bytes;
    }
}
