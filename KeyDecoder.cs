namespace WinKeyExtract;

/// <summary>
/// Decodes Windows product keys from DigitalProductId binary blobs stored in the registry.
/// Supports both legacy (pre-Win8) and modern (Win8+) encoding formats.
/// </summary>
public static class KeyDecoder
{
    private const int LegacyKeyOffset = 52;
    private const int Dpid4KeyOffset = 808; // DigitalProductId4 stores key data at offset 0x328
    private const int KeyLength = 15;
    private const int DecodeLength = 29; // 25 chars + 4 dashes
    private static readonly char[] Digits = "BCDFGHJKMPQRTVWXY2346789".ToCharArray();

    /// <summary>
    /// Decode a product key from a DigitalProductId byte array (legacy, offset 52).
    /// </summary>
    public static string? DecodeKey(byte[]? digitalProductId)
    {
        if (digitalProductId is null || digitalProductId.Length < LegacyKeyOffset + KeyLength)
            return null;

        string? key = DecodeKeyLegacy(digitalProductId, LegacyKeyOffset);
        return IsValidKey(key) ? key : null;
    }

    /// <summary>
    /// Decode a product key from a DigitalProductId4 byte array (Win8+, offset 808).
    /// </summary>
    public static string? DecodeKeyDpid4(byte[]? digitalProductId4)
    {
        if (digitalProductId4 is null || digitalProductId4.Length < Dpid4KeyOffset + KeyLength)
            return null;

        // Try Win8+ decoding first
        string? key = DecodeKeyWin8(digitalProductId4, Dpid4KeyOffset);
        if (IsValidKey(key))
            return key;

        // Fall back to legacy decode at the same offset
        key = DecodeKeyLegacy(digitalProductId4, Dpid4KeyOffset);
        return IsValidKey(key) ? key : null;
    }

    /// <summary>
    /// Win8+ decoding: the byte at offset 66 contains an extra bit indicating the new format.
    /// Uses base-24 decoding with an inserted 'N' character.
    /// </summary>
    private static string? DecodeKeyWin8(byte[] id, int offset)
    {
        try
        {
            // Work on a copy to avoid mutating the original
            byte[] key = new byte[KeyLength];
            Array.Copy(id, offset, key, 0, KeyLength);

            // Extract the "isWin8+" flag from the last byte's high bit
            int isWin8 = (key[KeyLength - 1] >> 3) & 1;
            key[KeyLength - 1] = (byte)((key[KeyLength - 1] & 0xF7) | ((isWin8 & 2) << 2));

            // Base-24 decode
            char[] chars = new char[25];
            for (int i = 24; i >= 0; i--)
            {
                int acc = 0;
                for (int j = KeyLength - 1; j >= 0; j--)
                {
                    int cur = acc * 256 ^ key[j];
                    key[j] = (byte)(cur / 24);
                    acc = cur % 24;
                }
                chars[i] = Digits[acc];
            }

            string decoded = new string(chars);

            // If Win8+ flag set, insert 'N' at the correct position
            if (isWin8 == 1)
            {
                int insertIndex = decoded.LastIndexOf(chars[0]);
                decoded = decoded[1..]; // Remove first char
                decoded = decoded.Insert(insertIndex, "N");
            }

            return FormatKey(decoded);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Legacy (pre-Win8) decoding: straightforward base-24 extraction.
    /// </summary>
    private static string? DecodeKeyLegacy(byte[] id, int offset)
    {
        try
        {
            byte[] key = new byte[KeyLength];
            Array.Copy(id, offset, key, 0, KeyLength);

            char[] chars = new char[25];
            for (int i = 24; i >= 0; i--)
            {
                int acc = 0;
                for (int j = KeyLength - 1; j >= 0; j--)
                {
                    int cur = (acc << 8) | key[j];
                    key[j] = (byte)(cur / 24);
                    acc = cur % 24;
                }
                chars[i] = Digits[acc];
            }

            return FormatKey(new string(chars));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Insert dashes every 5 characters: XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
    /// </summary>
    private static string FormatKey(string raw)
    {
        if (raw.Length != 25)
            return raw;

        return string.Join("-",
            raw[..5],
            raw[5..10],
            raw[10..15],
            raw[15..20],
            raw[20..25]);
    }

    /// <summary>
    /// Basic validity check: 29 chars, correct format with dashes, only valid characters.
    /// </summary>
    private static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length != DecodeLength)
            return false;

        for (int i = 0; i < key.Length; i++)
        {
            if ((i + 1) % 6 == 0)
            {
                if (key[i] != '-')
                    return false;
            }
            else
            {
                char c = key[i];
                if (c != 'N' && Array.IndexOf(Digits, c) < 0)
                    return false;
            }
        }

        return true;
    }
}
