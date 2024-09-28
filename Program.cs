using System.Text;

namespace Zippy;

/// <research>
/// https://en.wikipedia.org/wiki/Huffman_coding
/// https://en.wikipedia.org/wiki/Canonical_Huffman_code
/// </research>
public class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string originalText = "This is an example string for Huffman encoding. The more data provided, the better the compression.";

        #region [Text reading and writing]
        HuffmanCharTree hTree = new HuffmanCharTree();
        hTree.Build(originalText);

        Console.WriteLine();
        string compressed = hTree.Compress(originalText);
        Console.WriteLine($"⇒ Compressed(Binary): {compressed}\r\n");
        string decompressed = hTree.Decompress(compressed);
        Console.WriteLine($"⇒ Decompressed………………: {decompressed}\r\n");

        Console.WriteLine();
        string compressedBase64 = hTree.CompressToBase64(originalText);
        Console.WriteLine($"⇒ Compressed(Base64): {compressedBase64}\r\n");
        string decompressedBase64 = hTree.DecompressFromBase64(compressedBase64);
        Console.WriteLine($"⇒ Decompressed………………: {decompressedBase64}\r\n");
        #endregion

        #region [File saving and reading]
        Console.WriteLine("⇒ File compression using byte type…");
        HuffmanByteTree byteTree = new HuffmanByteTree();
        var someDataByte = File.ReadAllBytes("Zippy.exe");
        byteTree.CompressByteArrayToStream(someDataByte, "Compressed_Byte.bin");
        var decompByte = byteTree.DecompressByteArrayFromStream("Compressed_Byte.bin");
        File.WriteAllBytes($"Zippy_Byte.exe", decompByte);
        Console.WriteLine();

        Console.WriteLine("⇒ File compression using short type…");
        // You could use short for Unicode string applications.
        HuffmanShortTree shortTree = new HuffmanShortTree();
        var shortData = File.ReadAllBytes("Zippy.exe");
        // NOTE: For the compressed short bin file it will be larger since we are
        // storing 2 bytes per 1 byte, but the decompressed result will be the same.
        shortTree.CompressShortArrayToStream(HuffmanShortTree.ConvertByteArrayToShortArray(shortData), "Compressed_Short.bin");
        var decompShort = shortTree.DecompressShortArrayFromStream("Compressed_Short.bin");
        File.WriteAllBytes($"Zippy_Short.exe", HuffmanShortTree.ConvertShortArrayToByteArray(decompShort));
        Console.WriteLine();
        #endregion

        #region [String encoding demonstration]
        string strEnc = "♠ ♣ ♥ ♦ a b c € … •";
        byte[] byteEnc = strEnc.Select(c => (byte)c).ToArray();    // ⇦ does not encode the € properly
        short[] shortEnc = strEnc.Select(c => (short)c).ToArray(); // ⇦ does encode the € properly

        // NOTE: The short is an "System.Int16" which matches
        // the way C#/.NET encodes for Unicode representation.

        Console.WriteLine("⇒ Incorrect output to console…");
        foreach (var print in byteEnc) { Console.Write($"{(char)print}"); }
        Console.WriteLine("\r\n");

        Console.WriteLine("⇒ Correct output to console…");
        foreach (var print in shortEnc) { Console.Write($"{(char)print}"); }
        Console.WriteLine("\r\n");
        #endregion

        Console.WriteLine($"⇒ Test completed. Press any key to exit.");

        _ = Console.ReadKey(true);
    }
}
