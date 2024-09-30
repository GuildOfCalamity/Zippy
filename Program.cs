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
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleHandler);

        #region [Commandline functionality]
        if (args.Length == 2)
        {
            string inputFile = string.Empty;
            HuffmanByteTree hbt = new HuffmanByteTree();
            switch(args[0])
            {
                // Zipping switches
                case "c": case "-c":
                case "z": case "-z":
                    inputFile = args[1];
                    if (File.Exists(inputFile))
                    {
                        var ofl = new FileInfo(inputFile).Length;
                        if (ofl > 200_000_000) // 200MB
                            Console.WriteLine($"⇒ WARNING: Files this large (>200MB) should not be attempted.");
                        Console.WriteLine($"⇒ Compressing \"{inputFile}\"…");
                        var fileData = Retry(() => File.ReadAllBytes(inputFile));
                        hbt.CompressByteArrayToStream(fileData, $"{Path.GetFileNameWithoutExtension(inputFile)}.zipped");
                        Console.WriteLine($"⇒ Compression operation completed.");
                        var cfl = new FileInfo($"{Path.GetFileNameWithoutExtension(inputFile)}.zipped").Length;
                        Console.WriteLine($"⇒ Compression amount: {((float)cfl/(float)ofl)*100f:N1}%");
                    }
                    else
                    {
                        Console.WriteLine($"⇒ File \"{inputFile}\" could not be located.");
                    }
                    break;
                // Unzipping switches
                case "d": case "-d":
                case "u": case "-u":
                    inputFile = args[1];
                    if (File.Exists(inputFile))
                    {
                        var decomped = Retry(() => hbt.DecompressByteArrayFromStream($"{inputFile}"));
                        File.WriteAllBytes($"{Path.GetFileNameWithoutExtension(inputFile)}.unzipped", decomped);
                        Console.WriteLine($"⇒ Decompression operation completed.");
                    }
                    else
                    {
                        Console.WriteLine($"⇒ File \"{inputFile}\" could not be located.");
                    }
                    break;
                default:
                    Console.WriteLine($"⇒ Undefined switch or wrong argument order.");
                    Console.WriteLine($"⇒ EXAMPLE: \"Zippy -c SomeFileToCompress.txt\"");
                    Console.WriteLine($"⇒ EXAMPLE: \"Zippy -d SomeFileToDecompress.txt\"");
                    break;
            }
            Console.WriteLine($"⇒ Process completed. Press any key to exit.");
            _ = Console.ReadKey(true);
            return;
        }
        #endregion

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

    #region [Helpers]
    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine("⇒ UnhandledException Event!");
        Console.WriteLine($"⇒ Message: {(e.ExceptionObject as Exception)?.Message}");
    }

    protected static void ConsoleHandler(object? sender, ConsoleCancelEventArgs args)
    {
        Console.WriteLine();
        //$" Key pressed......: {args.SpecialKey}"
        //$" Cancel property..: {args.Cancel}"
        args.Cancel = false; // Setting the Cancel property to true will prevent the process from terminating.
        Thread.Sleep(500);
        Environment.Exit(-1);
    }

    /// <summary>
    ///   Generic retry mechanism with exponential back-off
    /// <example><code>
    ///   Retry(() => MethodThatHasNoReturnValue());
    /// </code></example>
    /// </summary>
    static void Retry(Action action, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                action();
                break;
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Console.WriteLine($"⇒ Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                Thread.Sleep(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }

    /// <summary>
    ///   Modified retry mechanism for return value with exponential back-off.
    /// <example><code>
    ///   int result = Retry(() => MethodThatReturnsAnInteger());
    /// </code></example>
    /// </summary>
    static T Retry<T>(Func<T> func, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Console.WriteLine($"⇒ Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                Thread.Sleep(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }
    #endregion
}
