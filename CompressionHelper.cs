using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zippy;

/// <research>
/// https://en.wikipedia.org/wiki/Huffman_coding
/// https://en.wikipedia.org/wiki/Canonical_Huffman_code
/// </research>

#region [Char Version]
public class HuffmanCharNode
{
    public char? Character { get; set; }
    public int Frequency { get; set; }
    public HuffmanCharNode Left { get; set; }
    public HuffmanCharNode Right { get; set; }
}

public class HuffmanCharTree
{
    HuffmanCharNode root;
    Dictionary<char, string> codes = new Dictionary<char, string>();
    bool noRepeatsForEncoding = false;

    /// <summary>
    /// Build the Huffman Tree from the input text
    /// </summary>
    public void Build(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Add repeating header for successful tree encoding (for non-repeat character strings).
        if (!text.StartsWith("000"))
            text = "000" + text;

        // Edge case: If text contains only one unique character, directly assign "0" as its Huffman code
        if (text.Distinct().Count() == 1 || text.Distinct().Count() == text.Length)
        {
            char uniqueChar = text[0];
            codes[uniqueChar] = "0";
            noRepeatsForEncoding = true;
            return;
        }
        else
        {
            noRepeatsForEncoding = false;
        }

        // Step 1: Calculate frequency of each character in the text
        var frequencies = text.GroupBy(c => c)
                              .ToDictionary(g => g.Key, g => g.Count());

        // Step 2: Create a priority queue (sorted list) to hold all nodes
        var priorityQueue = new SortedList<int, List<HuffmanCharNode>>();

        // Populate priority queue with leaf nodes
        foreach (var kvp in frequencies)
        {
            AddNodeToPriorityQueue(priorityQueue, new HuffmanCharNode { Character = kvp.Key, Frequency = kvp.Value });
        }

        // Step 3: Build the Huffman tree by merging the lowest frequency nodes
        while (priorityQueue.Count > 1)
        {
            // Remove two nodes with the lowest frequency
            var leftNode = RemoveMinNodeFromPriorityQueue(priorityQueue);
            var rightNode = RemoveMinNodeFromPriorityQueue(priorityQueue);

            // Create a new internal node with these two nodes as children
            var newNode = new HuffmanCharNode
            {
                Character = null, // Non-leaf node
                Frequency = leftNode.Frequency + rightNode.Frequency,
                Left = leftNode,
                Right = rightNode
            };

            // Add the new node back into the priority queue
            AddNodeToPriorityQueue(priorityQueue, newNode);
        }

        // Step 4: The remaining node is the root of the Huffman Tree
        root = priorityQueue.First().Value.First();

        // Step 5: Generate Huffman codes by traversing the tree
        GenerateCodes(root, "");
    }

    /// <summary>
    /// Adds a node to the priority queue based on its frequency
    /// </summary>
    void AddNodeToPriorityQueue(SortedList<int, List<HuffmanCharNode>> queue, HuffmanCharNode node)
    {
        if (!queue.ContainsKey(node.Frequency))
            queue[node.Frequency] = new List<HuffmanCharNode>();

        queue[node.Frequency].Add(node);
    }

    /// <summary>
    /// Removes the node with the lowest frequency from the priority queue
    /// </summary>
    HuffmanCharNode RemoveMinNodeFromPriorityQueue(SortedList<int, List<HuffmanCharNode>> queue)
    {
        var minFreq = queue.First().Key;
        var nodeList = queue[minFreq];
        var node = nodeList.First();
        nodeList.RemoveAt(0);

        if (nodeList.Count == 0)
            queue.Remove(minFreq);

        return node;
    }

    /// <summary>
    /// Generate Huffman codes for each character by traversing the tree
    /// </summary>
    void GenerateCodes(HuffmanCharNode node, string code)
    {
        if (node == null)
            return;

        if (node.Character.HasValue)
            codes[node.Character.Value] = code;

        GenerateCodes(node.Left, code + "0");
        GenerateCodes(node.Right, code + "1");
    }

    /// <summary>
    /// Compress the input text to a binary string using the Huffman codes.
    /// This can be used with a <see cref="System.IO.BinaryWriter"/> to efficiently write the data to disk.
    /// </summary>
    public string Compress(string text)
    {
        if (codes.Count == 0)
            throw new Exception("There are no HuffmanNodes in the tree. You must call Build first before any compress/decompress methods can be used.");

        if (noRepeatsForEncoding)
            return text;

        try
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in text)
            {
                if (codes.TryGetValue(ch, out string value))
                {
                    sb.Append(value);
                }
            }
            return sb.ToString();
        }
        catch (Exception ex) // System.Collections.Generic.KeyNotFoundException
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
        return text;
    }

    /// <summary>
    /// Compress the input text and encode it as Base64, embedding the bit length in the data
    /// </summary>
    public string CompressToBase64(string text)
    {
        if (codes.Count == 0)
            throw new Exception("There are no HuffmanNodes in the tree. You must call Build first before any compress/decompress methods can be used.");

        if (noRepeatsForEncoding)
            return text;

        try
        {   // Compress to binary string
            StringBuilder binaryBuilder = new StringBuilder();
            foreach (var ch in text) { binaryBuilder.Append(codes[ch]); }
            string binaryString = binaryBuilder.ToString();
            int originalBitLength = binaryString.Length; // Store the original bit length
            // Convert the binary string to a byte array
            byte[] byteArray = ConvertBinaryStringToByteArray(binaryString);
            // Create a new array to hold the original bit length and the compressed data
            byte[] resultArray = new byte[byteArray.Length + 4]; // 4 bytes for the length (int)
            // Store the bit length in the first 4 bytes
            byte[] bitLengthBytes = BitConverter.GetBytes(originalBitLength);
            if (BitConverter.IsLittleEndian) Array.Reverse(bitLengthBytes); // Ensure big-endian order
            Array.Copy(bitLengthBytes, resultArray, 4);
            // Copy the compressed data after the bit length
            Array.Copy(byteArray, 0, resultArray, 4, byteArray.Length);
            // Encode the result array to Base64
            return Convert.ToBase64String(resultArray);
        }
        catch (Exception ex) // System.Collections.Generic.KeyNotFoundException
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
        return text;
    }

    /// <summary>
    /// Decompress from Base64, automatically extracting the bit length from the encoded data
    /// </summary>
    public string DecompressFromBase64(string base64Text)
    {
        if (noRepeatsForEncoding)
            return base64Text;

        try
        {   // Decode the Base64 string to a byte array
            byte[] resultArray = Convert.FromBase64String(base64Text);
            // Extract the original bit length from the first 4 bytes
            byte[] bitLengthBytes = new byte[4];
            Array.Copy(resultArray, bitLengthBytes, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bitLengthBytes); // Ensure big-endian order
            int originalBitLength = BitConverter.ToInt32(bitLengthBytes, 0);
            // Extract the compressed data after the bit length
            byte[] byteArray = new byte[resultArray.Length - 4];
            Array.Copy(resultArray, 4, byteArray, 0, byteArray.Length);
            // Convert byte array back to a binary string, truncated to the original bit length
            string binaryString = ConvertByteArrayToBinaryString(byteArray, originalBitLength);
            // Decompress the binary string using the Huffman tree
            return Decompress(binaryString);
        }
        catch (Exception ex) // FormatException
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
        return base64Text;
    }

    /// <summary>
    /// Decompress the binary string back to the original text
    /// </summary>
    public string Decompress(string binaryText)
    {
        if (noRepeatsForEncoding)
            return binaryText;

        StringBuilder sb = new StringBuilder();
        HuffmanCharNode currentNode = root;

        foreach (var bit in binaryText)
        {
            if (currentNode != null && currentNode.Left != null && currentNode.Right != null)
            {
                currentNode = bit == '0' ? currentNode.Left : currentNode.Right;
                if (currentNode.Left == null && currentNode.Right == null) // Leaf node
                {
                    sb.Append(currentNode.Character.Value);
                    currentNode = root;
                }
            }
        }

        var result = $"{sb}";
        if (result.StartsWith("000"))
            return result.Substring(3);
        else
            return result;
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressToStream(string text, string fileName)
    {
        try
        {
            // Compress the text and write it to a file using BinaryWriter
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    CompressToStream(text, bw);
                }
            }
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressToStream(string text, BinaryWriter writer)
    {
        // First build the Huffman tree for the given text
        Build(text);
        // Write the character-to-binary-code mappings (Huffman tree)
        writer.Write(codes.Count);  // Number of mappings
        foreach (var kvp in codes)
        {
            writer.Write(kvp.Key);   // Character
            writer.Write(kvp.Value); // Corresponding binary code
        }
        // Compress the text to a binary string
        string binaryString = Compress(text);
        // Convert the binary string to a byte array
        byte[] byteArray = ConvertBinaryStringToByteArray(binaryString);
        // Write the compressed data length (in bytes) and the exact bit length
        writer.Write(byteArray.Length); // Number of bytes
        writer.Write(binaryString.Length); // Exact number of bits used
        // Write the compressed byte array
        writer.Write(byteArray);
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public string DecompressFromStream(string fileName)
    {
        string result = string.Empty;
        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    result = DecompressFromStream(reader);
                }
            }
        }
        catch (Exception) { }
        return result;
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public string DecompressFromStream(BinaryReader reader)
    {
        // Read the Huffman tree (character-to-binary-code mappings)
        int mappingsCount = reader.ReadInt32();  // Number of mappings
        codes = new Dictionary<char, string>();

        for (int i = 0; i < mappingsCount; i++)
        {
            char character = reader.ReadChar();  // Character
            string code = reader.ReadString();   // Binary code
            codes[character] = code;
        }

        // Rebuild the Huffman tree from the mappings
        RebuildTreeFromMappings();

        // Read the compressed data
        int byteArrayLength = reader.ReadInt32();   // Number of bytes in the compressed data
        int bitLength = reader.ReadInt32();         // Number of valid bits in the binary string
        byte[] byteArray = reader.ReadBytes(byteArrayLength);

        // Convert the byte array back to a binary string
        string binaryString = ConvertByteArrayToBinaryString(byteArray, bitLength);

        // Decompress the binary string
        return Decompress(binaryString);
    }

    /// <summary>
    /// Rebuild the Huffman tree from the character-to-binary-code mappings
    /// </summary>
    void RebuildTreeFromMappings()
    {
        root = new HuffmanCharNode();
        foreach (var kvp in codes)
        {
            InsertIntoTree(root, kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Helper method to insert a character into the Huffman tree using its binary code
    /// </summary>
    void InsertIntoTree(HuffmanCharNode node, char character, string code)
    {
        foreach (char c in code)
        {
            if (c == '0')
            {
                if (node.Left == null)
                    node.Left = new HuffmanCharNode();

                node = node.Left;
            }
            else
            {
                if (node.Right == null)
                    node.Right = new HuffmanCharNode();

                node = node.Right;
            }
        }
        node.Character = character;
    }

    /// <summary>
    /// Convert a binary string (e.g., "101010") into a byte array
    /// </summary>
    byte[] ConvertBinaryStringToByteArray(string binaryString)
    {
        int numOfBytes = (binaryString.Length + 7) / 8; // +7 to round up to the nearest byte
        byte[] byteArray = new byte[numOfBytes];
        for (int i = 0; i < binaryString.Length; i++)
        {
            if (binaryString[i] == '1')
            {
                byteArray[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }
        }
        return byteArray;
    }

    /// <summary>
    /// Convert a byte array back into a binary string, truncated to the original length
    /// </summary>
    string ConvertByteArrayToBinaryString(byte[] byteArray, int originalBitLength)
    {
        StringBuilder binaryStringBuilder = new StringBuilder();
        foreach (var b in byteArray)
        {
            binaryStringBuilder.Append(Convert.ToString(b, 2).PadLeft(8, '0')); // Ensure 8 bits per byte
        }
        // Truncate the binary string to the original bit length
        return binaryStringBuilder.ToString().Substring(0, originalBitLength);
    }
}
#endregion

#region [Byte Version]
public class HuffmanByteNode
{
    public byte? ByteValue { get; set; }
    public int Frequency { get; set; }
    public HuffmanByteNode Left { get; set; }
    public HuffmanByteNode Right { get; set; }
}

/// <summary>
/// To load a character string into the system from a byte stream you need to know 
/// the source encoding to therefore interpret and subsequently translate it correctly 
/// (otherwise the codes will be taken as already being in the system's default encoding 
/// and thus render gibberish). Similarly, when a string is written to an external source, 
/// it will be written in a particular encoding.
/// </summary>
public class HuffmanByteTree
{
    HuffmanByteNode root;
    Dictionary<byte, string> codes = new Dictionary<byte, string>();

    /// <summary>
    /// Build the Huffman Tree from the input byte array
    /// </summary>
    public void Build(byte[] data)
    {
        // Step 1: Calculate frequency of each byte in the array
        var frequencies = data.GroupBy(b => b)
                              .ToDictionary(g => g.Key, g => g.Count());

        // Step 2: Create a priority queue (sorted list) to hold all nodes
        var priorityQueue = new SortedList<int, List<HuffmanByteNode>>();

        // Populate priority queue with leaf nodes (each distinct byte)
        foreach (var kvp in frequencies)
        {
            var node = new HuffmanByteNode { ByteValue = kvp.Key, Frequency = kvp.Value };
            AddNodeToPriorityQueue(priorityQueue, node);
        }

        // If there's only one distinct byte, we need to handle it separately
        if (priorityQueue.Count == 1)
        {
            var singleNode = priorityQueue.First().Value.First();
            codes[singleNode.ByteValue.Value] = "0";  // Assign a default code
            return;
        }

        // Step 3: Build the Huffman tree by merging the lowest frequency nodes
        while (priorityQueue.Count > 1)
        {
            // Remove two nodes with the lowest frequency
            var leftNode = RemoveMinNodeFromPriorityQueue(priorityQueue);
            var rightNode = RemoveMinNodeFromPriorityQueue(priorityQueue);

            // Create a new internal node with these two nodes as children
            var newNode = new HuffmanByteNode
            {
                Frequency = leftNode.Frequency + rightNode.Frequency,
                Left = leftNode,
                Right = rightNode
            };

            // Add the new node back into the priority queue
            AddNodeToPriorityQueue(priorityQueue, newNode);
        }

        // Step 4: The remaining node is the root of the Huffman Tree
        root = priorityQueue.First().Value.First();

        // Step 5: Generate Huffman codes by traversing the tree
        GenerateCodes(root, "");
    }

    /// <summary>
    /// Build the Huffman Tree from the input string
    /// </summary>
    public void Build(string text, Encoding? enc)
    {
        if (enc is null)
            enc = Encoding.UTF8;

        var data = enc.GetBytes(text);

        // Step 1: Calculate frequency of each byte in the array
        var frequencies = data.GroupBy(b => b)
                              .ToDictionary(g => g.Key, g => g.Count());

        // Step 2: Create a priority queue (sorted list) to hold all nodes
        var priorityQueue = new SortedList<int, List<HuffmanByteNode>>();

        // Populate priority queue with leaf nodes (each distinct byte)
        foreach (var kvp in frequencies)
        {
            var node = new HuffmanByteNode { ByteValue = kvp.Key, Frequency = kvp.Value };
            AddNodeToPriorityQueue(priorityQueue, node);
        }

        // If there's only one distinct byte, we need to handle it separately
        if (priorityQueue.Count == 1)
        {
            var singleNode = priorityQueue.First().Value.First();
            codes[singleNode.ByteValue.Value] = "0";  // Assign a default code
            return;
        }

        // Step 3: Build the Huffman tree by merging the lowest frequency nodes
        while (priorityQueue.Count > 1)
        {
            // Remove two nodes with the lowest frequency
            var leftNode = RemoveMinNodeFromPriorityQueue(priorityQueue);
            var rightNode = RemoveMinNodeFromPriorityQueue(priorityQueue);

            // Create a new internal node with these two nodes as children
            var newNode = new HuffmanByteNode
            {
                Frequency = leftNode.Frequency + rightNode.Frequency,
                Left = leftNode,
                Right = rightNode
            };

            // Add the new node back into the priority queue
            AddNodeToPriorityQueue(priorityQueue, newNode);
        }

        // Step 4: The remaining node is the root of the Huffman Tree
        root = priorityQueue.First().Value.First();

        // Step 5: Generate Huffman codes by traversing the tree
        GenerateCodes(root, "");
    }

    /// <summary>
    /// Adds a node to the priority queue based on its frequency
    /// </summary>
    void AddNodeToPriorityQueue(SortedList<int, List<HuffmanByteNode>> queue, HuffmanByteNode node)
    {
        if (!queue.ContainsKey(node.Frequency))
            queue[node.Frequency] = new List<HuffmanByteNode>();

        queue[node.Frequency].Add(node);
    }

    /// <summary>
    /// Removes the node with the lowest frequency from the priority queue
    /// </summary>
    HuffmanByteNode RemoveMinNodeFromPriorityQueue(SortedList<int, List<HuffmanByteNode>> queue)
    {
        var minFreq = queue.First().Key;
        var nodeList = queue[minFreq];
        var node = nodeList.First();
        nodeList.RemoveAt(0);

        if (nodeList.Count == 0)
            queue.Remove(minFreq);

        return node;
    }

    /// <summary>
    /// Generate Huffman codes for each byte by traversing the tree
    /// </summary>
    void GenerateCodes(HuffmanByteNode node, string code)
    {
        if (node == null)
            return;

        if (node.ByteValue.HasValue)
        {
            codes[node.ByteValue.Value] = code;
        }

        GenerateCodes(node.Left, code + "0");
        GenerateCodes(node.Right, code + "1");
    }

    /// <summary>
    /// Compress the input byte array and return the binary string representation
    /// </summary>
    public string Compress(byte[] data)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var b in data)
        {
            if (codes.TryGetValue(b, out string? value))
            {
                sb.Append(value);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressByteArrayToStream(byte[] data, string fileName)
    {
        try
        {
            // Compress the text and write it to a file using BinaryWriter
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    CompressByteArrayToStream(data, bw);
                }
            }
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressByteArrayToStream(byte[] data, BinaryWriter writer)
    {
        // First build the Huffman tree for the given byte array
        Build(data);

        if (codes.Count == 0)
            throw new Exception("There are no Huffman codes available for writing. If allowed to continue, this will result in a zero-byte file.");

        // Write the byte-to-binary-code mappings (Huffman tree)
        writer.Write(codes.Count);  // Number of mappings
        foreach (var kvp in codes)
        {
            writer.Write(kvp.Key);         // Byte
            writer.Write(kvp.Value);       // Corresponding binary code
        }

        // Compress the byte array to a binary string
        string binaryString = Compress(data);

        // Convert the binary string to a byte array
        byte[] byteArray = ConvertBinaryStringToByteArray(binaryString);

        // Write the compressed data length (in bytes) and the exact bit length
        writer.Write(byteArray.Length); // Number of bytes
        writer.Write(binaryString.Length); // Exact number of bits used

        // Write the compressed byte array
        writer.Write(byteArray);
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public byte[] DecompressByteArrayFromStream(string fileName)
    {
        byte[] result;
        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    result = DecompressByteArrayFromStream(reader);
                }
            }
        }
        catch (Exception) { result = []; }
        return result;
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public byte[] DecompressByteArrayFromStream(BinaryReader reader)
    {
        // Read the Huffman tree (byte-to-binary-code mappings)
        int mappingsCount = reader.ReadInt32();  // Number of mappings
        codes = new Dictionary<byte, string>();

        for (int i = 0; i < mappingsCount; i++)
        {
            try
            {
                byte byteValue = reader.ReadByte();  // Byte
                string code = reader.ReadString();   // Binary code
                codes[byteValue] = code;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] DecompressByteArrayFromStream: {ex.Message}");
            }
        }

        // Rebuild the Huffman tree from the mappings
        RebuildTreeFromMappings();

        // Read the compressed data
        int byteArrayLength = reader.ReadInt32();   // Number of bytes in the compressed data
        int bitLength = reader.ReadInt32();         // Number of valid bits in the binary string
        byte[] byteArray = reader.ReadBytes(byteArrayLength);

        // Convert the byte array back to a binary string
        string binaryString = ConvertByteArrayToBinaryString(byteArray, bitLength);

        // Decompress the binary string into the original byte array
        return Decompress(binaryString);
    }

    /// <summary>
    /// Rebuild the Huffman tree from the byte-to-binary-code mappings
    /// </summary>
    void RebuildTreeFromMappings()
    {
        root = new HuffmanByteNode();
        foreach (var kvp in codes)
        {
            InsertIntoTree(root, kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Helper method to insert a byte into the Huffman tree using its binary code
    /// </summary>
    void InsertIntoTree(HuffmanByteNode node, byte byteValue, string code)
    {
        foreach (char c in code)
        {
            if (c == '0')
            {
                if (node.Left == null)
                    node.Left = new HuffmanByteNode();
                node = node.Left;
            }
            else
            {
                if (node.Right == null)
                    node.Right = new HuffmanByteNode();
                node = node.Right;
            }
        }
        node.ByteValue = byteValue;
    }

    /// <summary>
    /// Decompress the binary string back to the original byte array
    /// </summary>
    byte[] Decompress(string binaryText)
    {
        List<byte> byteList = new List<byte>();
        HuffmanByteNode currentNode = root;

        foreach (var bit in binaryText)
        {
            currentNode = bit == '0' ? currentNode.Left : currentNode.Right;

            if (currentNode.Left == null && currentNode.Right == null) // Leaf node
            {
                byteList.Add(currentNode.ByteValue.Value);
                currentNode = root;
            }
        }

        return byteList.ToArray();
    }

    /// <summary>
    /// Convert a binary string (e.g., "101010") into a byte array
    /// </summary>
    byte[] ConvertBinaryStringToByteArray(string binaryString)
    {
        int numOfBytes = (binaryString.Length + 7) / 8; // +7 to round up to the nearest byte
        byte[] byteArray = new byte[numOfBytes];

        for (int i = 0; i < binaryString.Length; i++)
        {
            if (binaryString[i] == '1')
            {
                byteArray[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }
        }

        return byteArray;
    }

    /// <summary>
    /// Convert a byte array back into a binary string, truncate to bitLength
    /// </summary>
    string ConvertByteArrayToBinaryString(byte[] byteArray, int bitLength)
    {
        StringBuilder binaryStringBuilder = new StringBuilder();

        foreach (var b in byteArray)
        {
            binaryStringBuilder.Append(Convert.ToString(b, 2).PadLeft(8, '0')); // Ensure 8 bits per byte
        }

        // Truncate the binary string to the exact bit length
        return binaryStringBuilder.ToString().Substring(0, bitLength);
    }
}

#endregion

#region [Short Version]
public class HuffmanShortNode
{
    public short? ShortValue { get; set; } // System.Int16
    public int Frequency { get; set; }
    public HuffmanShortNode Left { get; set; }
    public HuffmanShortNode Right { get; set; }
}

public class HuffmanShortTree
{
    HuffmanShortNode root;
    Dictionary<short, string> codes = new Dictionary<short, string>();

    /// <summary>
    /// Build the Huffman Tree from the input short array
    /// </summary>
    public void Build(short[] data)
    {
        // Step 1: Calculate frequency of each byte in the array
        var frequencies = data.GroupBy(b => b)
                              .ToDictionary(g => g.Key, g => g.Count());

        // Step 2: Create a priority queue (sorted list) to hold all nodes
        var priorityQueue = new SortedList<int, List<HuffmanShortNode>>();

        // Populate priority queue with leaf nodes (each distinct byte)
        foreach (var kvp in frequencies)
        {
            var node = new HuffmanShortNode { ShortValue = kvp.Key, Frequency = kvp.Value };
            AddNodeToPriorityQueue(priorityQueue, node);
        }

        // If there's only one distinct byte, we need to handle it separately
        if (priorityQueue.Count == 1)
        {
            var singleNode = priorityQueue.First().Value.First();
            codes[singleNode.ShortValue.Value] = "0";  // Assign a default code
            return;
        }

        // Step 3: Build the Huffman tree by merging the lowest frequency nodes
        while (priorityQueue.Count > 1)
        {
            // Remove two nodes with the lowest frequency
            var leftNode = RemoveMinNodeFromPriorityQueue(priorityQueue);
            var rightNode = RemoveMinNodeFromPriorityQueue(priorityQueue);

            // Create a new internal node with these two nodes as children
            var newNode = new HuffmanShortNode
            {
                Frequency = leftNode.Frequency + rightNode.Frequency,
                Left = leftNode,
                Right = rightNode
            };

            // Add the new node back into the priority queue
            AddNodeToPriorityQueue(priorityQueue, newNode);
        }

        // Step 4: The remaining node is the root of the Huffman Tree
        root = priorityQueue.First().Value.First();

        // Step 5: Generate Huffman codes by traversing the tree
        GenerateCodes(root, "");
    }

    /// <summary>
    /// ORIGINAL - Adds a node to the priority queue based on its frequency
    /// </summary>
    //void AddNodeToPriorityQueue(SortedList<int, List<HuffmanShortNode>> queue, HuffmanShortNode node)
    //{
    //    int frequency = 1;  // Since we only care about the structure, assign frequency = 1 for unique shorts
    //    if (!queue.ContainsKey(frequency))
    //        queue[frequency] = new List<HuffmanShortNode>();
    //
    //    queue[frequency].Add(node);
    //}

    /// <summary>
    /// MODIFIED - Adds a node to the priority queue based on its frequency
    /// </summary>
    void AddNodeToPriorityQueue(SortedList<int, List<HuffmanShortNode>> queue, HuffmanShortNode node)
    {
        if (!queue.ContainsKey(node.Frequency))
            queue[node.Frequency] = new List<HuffmanShortNode>();

        queue[node.Frequency].Add(node);
    }


    /// <summary>
    /// Removes the node with the lowest frequency from the priority queue
    /// </summary>
    HuffmanShortNode RemoveMinNodeFromPriorityQueue(SortedList<int, List<HuffmanShortNode>> queue)
    {
        var minFreq = queue.First().Key;
        var nodeList = queue[minFreq];
        var node = nodeList.First();
        nodeList.RemoveAt(0);

        if (nodeList.Count == 0)
            queue.Remove(minFreq);

        return node;
    }

    /// <summary>
    /// Generate Huffman codes for each short value by traversing the tree
    /// </summary>
    void GenerateCodes(HuffmanShortNode node, string code)
    {
        if (node == null)
            return;

        if (node.ShortValue.HasValue)
        {
            codes[node.ShortValue.Value] = code;
        }

        GenerateCodes(node.Left, code + "0");
        GenerateCodes(node.Right, code + "1");
    }

    /// <summary>
    /// Compress the input short array and return the binary string representation
    /// </summary>
    public string Compress(short[] data)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var s in data)
        {
            if (codes.TryGetValue(s, out string? value))
            {
                sb.Append(value);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressShortArrayToStream(short[] data, string fileName)
    {
        try
        {
            // Compress the text and write it to a file using BinaryWriter
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    CompressShortArrayToStream(data, bw);
                }
            }
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Compress and write the Huffman tree and compressed binary data using BinaryWriter
    /// </summary>
    public void CompressShortArrayToStream(short[] data, BinaryWriter writer)
    {
        // First build the Huffman tree for the given short array
        Build(data);

        // Write the short-to-binary-code mappings (Huffman tree)
        writer.Write(codes.Count);  // Number of mappings
        foreach (var kvp in codes)
        {
            writer.Write(kvp.Key);         // Short (16-bit value)
            writer.Write(kvp.Value);       // Corresponding binary code
        }

        // Compress the short array to a binary string
        string binaryString = Compress(data);

        // Convert the binary string to a byte array
        byte[] byteArray = ConvertBinaryStringToByteArray(binaryString);

        // Write the compressed data length (in bytes) and the exact bit length
        writer.Write(byteArray.Length); // Number of bytes
        writer.Write(binaryString.Length); // Exact number of bits used

        // Write the compressed byte array
        writer.Write(byteArray);
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public short[] DecompressShortArrayFromStream(string fileName)
    {
        short[] result;
        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    result = DecompressShortArrayFromStream(reader);
                }
            }
        }
        catch (Exception) { result = []; }
        return result;
    }

    /// <summary>
    /// Read the Huffman tree and compressed binary data using BinaryReader and decompress it
    /// </summary>
    public short[] DecompressShortArrayFromStream(BinaryReader reader)
    {
        // Read the Huffman tree (short-to-binary-code mappings)
        int mappingsCount = reader.ReadInt32();  // Number of mappings
        codes = new Dictionary<short, string>();

        for (int i = 0; i < mappingsCount; i++)
        {
            short shortValue = reader.ReadInt16();  // Short (16-bit value)
            string code = reader.ReadString();   // Binary code
            codes[shortValue] = code;
        }

        // Rebuild the Huffman tree from the mappings
        RebuildTreeFromMappings();

        // Read the compressed data
        int byteArrayLength = reader.ReadInt32();   // Number of bytes in the compressed data
        int bitLength = reader.ReadInt32();         // Number of valid bits in the binary string
        byte[] byteArray = reader.ReadBytes(byteArrayLength);

        // Convert the byte array back to a binary string
        string binaryString = ConvertByteArrayToBinaryString(byteArray, bitLength);

        // Decompress the binary string into the original short array
        return Decompress(binaryString);
    }

    /// <summary>
    /// Rebuild the Huffman tree from the short-to-binary-code mappings
    /// </summary>
    void RebuildTreeFromMappings()
    {
        root = new HuffmanShortNode();

        foreach (var kvp in codes)
        {
            InsertIntoTree(root, kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Helper method to insert a short value into the Huffman tree using its binary code
    /// </summary>
    void InsertIntoTree(HuffmanShortNode node, short shortValue, string code)
    {
        foreach (char c in code)
        {
            if (c == '0')
            {
                if (node.Left == null)
                    node.Left = new HuffmanShortNode();
                node = node.Left;
            }
            else
            {
                if (node.Right == null)
                    node.Right = new HuffmanShortNode();
                node = node.Right;
            }
        }
        node.ShortValue = shortValue;
    }

    /// <summary>
    /// Decompress the binary string back to the original short array
    /// </summary>
    short[] Decompress(string binaryText)
    {
        List<short> shortList = new List<short>();
        HuffmanShortNode currentNode = root;

        foreach (var bit in binaryText)
        {
            currentNode = bit == '0' ? currentNode.Left : currentNode.Right;

            if (currentNode.Left == null && currentNode.Right == null) // Leaf node
            {
                shortList.Add(currentNode.ShortValue.Value);
                currentNode = root;
            }
        }

        return shortList.ToArray();
    }

    /// <summary>
    /// Convert a binary string (e.g., "101010") into a byte array
    /// </summary>
    byte[] ConvertBinaryStringToByteArray(string binaryString)
    {
        int numOfBytes = (binaryString.Length + 7) / 8; // +7 to round up to the nearest byte
        byte[] byteArray = new byte[numOfBytes];

        for (int i = 0; i < binaryString.Length; i++)
        {
            if (binaryString[i] == '1')
            {
                byteArray[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }
        }

        return byteArray;
    }

    /// <summary>
    /// Convert a byte array back into a binary string, truncate to bitLength
    /// </summary>
    string ConvertByteArrayToBinaryString(byte[] byteArray, int bitLength)
    {
        StringBuilder binaryStringBuilder = new StringBuilder();

        foreach (var b in byteArray)
        {
            binaryStringBuilder.Append(Convert.ToString(b, 2).PadLeft(8, '0')); // Ensure 8 bits per byte
        }

        // Truncate the binary string to the exact bit length
        return binaryStringBuilder.ToString().Substring(0, bitLength);
    }

    /// <summary>
    /// Convert byte array to short array
    /// </summary>
    public static short[] ConvertByteArrayToShortArray(byte[] byteArray)
    {
        int numOfShorts = byteArray.Length / 2;
        short[] shortArray = new short[numOfShorts];

        for (int i = 0; i < numOfShorts; i++)
        {
            shortArray[i] = BitConverter.ToInt16(byteArray, i * 2);
        }

        return shortArray;
    }

    /// <summary>
    /// Convert short array to byte array
    /// </summary>
    public static byte[] ConvertShortArrayToByteArray(short[] shortArray)
    {
        byte[] byteArray = new byte[shortArray.Length * 2];

        for (int i = 0; i < shortArray.Length; i++)
        {
            byte[] shortBytes = BitConverter.GetBytes(shortArray[i]);
            byteArray[i * 2] = shortBytes[0];
            byteArray[i * 2 + 1] = shortBytes[1];
        }

        return byteArray;
    }
}
#endregion
