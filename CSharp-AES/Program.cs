using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

internal static class Program
{
    private static readonly byte[] KeyA =
    [
        0x2B, 0x7E, 0x15, 0x16, 0x28, 0xAE, 0xD2, 0xA6,
        0xAB, 0xF7, 0x15, 0x88, 0x09, 0xCF, 0x4F, 0x3C,
        0x76, 0x2E, 0x71, 0x60, 0xF3, 0x8B, 0x4D, 0xA5,
        0x6A, 0x78, 0x4D, 0x90, 0x45, 0x19, 0x0C, 0xFE
    ];

    private static readonly byte[] Iv =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
    ];

#if REAL_KEY
    // Stomping target A: native R2R code inlines this; stomper guts the IL body
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] GetRealKey() =>
    [
        0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE,
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
        0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF
    ];

    // Stomping target B: native R2R code inlines this; stomper guts the IL body
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHiddenMessage() =>
        Console.WriteLine("This is hidden message");
#endif

    static void Main(string[] args)
    {
#if REAL_KEY
        WriteHiddenMessage();
        byte[] key = GetRealKey();
#else
        byte[] key = KeyA;
#endif

        string input = args.Length > 0
            ? string.Join(" ", args)
            : Console.ReadLine() ?? string.Empty;

        byte[] plaintext = Encoding.UTF8.GetBytes(input);

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        Console.WriteLine(Convert.ToBase64String(ciphertext));
    }
}
