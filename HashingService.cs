using System.Security.Cryptography;
using System.Text;

namespace FintechBlockchain;

public sealed class HashingService
{
    public const string UkrainianAlphabet = "абвгдеєжзиіїйклмнопрстуфхцчшщьюя";
    public const string LatinAlphabet = "abcdefghijklmnopqrstuvwxyz234567";

    public string ComputeHash(string data)
    {
        return Convert.ToHexStringLower(ComputeDigest(data));
    }

    public string ComputeHash(string data, long nonce)
    {
        return ComputeHash($"{data}|{nonce}");
    }

    public string ComputeHash(Block block)
    {
        string blockData = $"{block.Index}|{block.TimeStamp:O}|{block.Data}|{block.PreviousHash}|{block.DifficultyAtMining}";
        return ComputeHash(blockData, block.Nonce);
    }

    public string ComputeLetterHash(string data, long nonce, string alphabet)
    {
        byte[] digest = ComputeDigest($"{data}|{nonce}");
        char[] encoded = new char[(digest.Length * 8 + 4) / 5];
        int buffer = 0;
        int bits = 0;
        int index = 0;

        foreach (byte value in digest)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                encoded[index++] = alphabet[(buffer >> bits) & 31];
            }
            buffer &= (1 << bits) - 1;
        }

        if (bits > 0)
        {
            encoded[index] = alphabet[buffer << (5 - bits)];
        }

        encoded[0] = char.ToUpperInvariant(encoded[0]);
        return new string(encoded);
    }

    private static byte[] ComputeDigest(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SHA256.HashData(Encoding.UTF8.GetBytes(data));
    }
}
