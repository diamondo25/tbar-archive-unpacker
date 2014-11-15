using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using SharpLZW;

namespace TBAR_Archive_Unpacker
{
    struct HashTableElement
    {
        public int fileId;
        public int elementId;
        public uint specialHash;
        public int offsetInFile;
        public int sizeInFile;
        public int checksum;

        public byte[] contents;
    }


    class Program
    {
        static Dictionary<int, HashTableElement> AllElements = new Dictionary<int, HashTableElement>();


        static void Main(string[] args)
        {


            Directory.CreateDirectory("output");
            string folder = @"I:\SteamLibrary\SteamApps\common\The Binding of Isaac Rebirth\resources\packed\";
            string[] files = new string[] { "config", "animations", "graphics", "fonts", "music", "rooms", "sfx", "videos" };

            for (var i = 0; i < files.Length; i++)
            {
                var elements = LoadFile(folder + files[i] + ".a", i);
                /*
                foreach (var element in elements)
                {
                    var id = element.elementId & 0x7FFF;
                    while (AllElements.ContainsKey(id))
                    {
                        // Try to find an ID that is in bounds
                        id++; // ...
                        id &= 0x7FFF;
                    }
                    AllElements.Add(id, element);

                }
                */
            }

            Console.ReadLine();
        }

        static uint Crypto(uint xorKey, byte[] data)
        {
            for (var i = 0; i < 1024; i += 4)
            {
                {
                    var xorOne = BitConverter.ToUInt32(data, i) ^ xorKey;
                    Buffer.BlockCopy(BitConverter.GetBytes(xorOne), 0, data, i, 4);
                }

                var mode = (xorKey & 0xF);
                if (mode == 2)
                {
                    var v10 = data[i + 3];
                    data[i + 3] = data[i + 0];
                    var v11 = data[i + 1];
                    data[i + 0] = v10;
                    var v6 = data[i + 2];
                    data[i + 2] = v11;
                    data[i + 1] = v6;
                }
                else if (mode == 9)
                {
                    var v7 = data[i + 0];
                    data[i + 0] = data[i + 1];
                    var v8 = data[i + 3];
                    data[i + 1] = v7;
                    var v9 = data[i + 2];
                    data[i + 2] = v8;
                    data[i + 3] = v9;
                }
                else if (mode == 13)
                {
                    var v4 = data[i + 2];
                    data[i + 2] = data[i + 0];
                    var v5 = data[i + 1];
                    data[i + 0] = v4;
                    var v6 = data[i + 3];
                    data[i + 3] = v5;
                    data[i + 1] = v6;
                }

                xorKey ^= (((xorKey ^ (xorKey << 8)) >> 9) ^ (xorKey << 8) ^ ((((xorKey ^ (xorKey << 8)) >> 9) ^ xorKey ^ (xorKey << 8)) << 23));

            }

            return xorKey;
        }


        private const int NumBytesPerCode = 2;
        static int ReadCode(BinaryReader reader)
        {
            int code = 0;
            int shift = 0;

            for (int i = 0; i < NumBytesPerCode; i++)
            {
                byte nextByte = reader.ReadByte();
                code += nextByte << shift;
                shift += 8;
            }

            return code;
        }

        static int CalculateFilenameHash(string filename)
        {
            int result = 5381;
            for (var i = 0; i < filename.Length; i++)
            {
                sbyte c = (sbyte)filename[i];
                if ((c - 65) < 25)
                {
                    c += 32;
                }

                if ((char)c == '\\')
                    c = (sbyte)'/';

                int tmp = result * 33;

                result = tmp + (byte)c;
            }

            return result;
        }

        static List<HashTableElement> LoadFile(string path, int fileId)
        {
            using (var br = new BinaryReader(File.OpenRead(path)))
            {
                if ("ARCH000" != Encoding.ASCII.GetString(br.ReadBytes(7)))
                {
                    Console.WriteLine("Not a TBAR file");
                    Environment.Exit(1);
                }

                bool compressed = br.ReadByte() == 1;
                int offset = br.ReadInt32();
                short amount = br.ReadInt16();

                br.BaseStream.Position = offset;

                List<HashTableElement> elements = new List<HashTableElement>(amount);

                for (var i = 0; i < amount; i++)
                {
                    var element = new HashTableElement();
                    element.fileId = fileId;
                    element.elementId = br.ReadInt32();
                    element.specialHash = br.ReadUInt32();
                    element.offsetInFile = br.ReadInt32();
                    element.sizeInFile = br.ReadInt32();
                    element.checksum = br.ReadInt32();

                    elements.Add(element);
                }

                for (var i = 0; i < amount; i++)
                {
                    var element = elements[i];
                    br.BaseStream.Position = element.offsetInFile;


                    element.contents = br.ReadBytes(element.sizeInFile);
                    elements[i] = element;

                    byte[] cleanData;

                    if (compressed)
                    {
                        uint hur = (uint)(element.elementId);
                        //if (hur != (uint)402372392)
                        //    continue;
                        File.WriteAllBytes(Path.Combine("output", hur + "_RAW.bin"), element.contents);

                        cleanData = new byte[4];
                        int cleanDataOffset = 0;
                        LZWDecoder decoder = new LZWDecoder();

                        bool nosave = false;
                        string buffertje = "";

                        for (var j = 0; j < element.sizeInFile; )
                        {
                            try
                            {
                                int len = BitConverter.ToInt32(element.contents, j);
                                j += 4;
                                byte[] blah = new byte[len];
                                Console.WriteLine("Reading blob {0} - {1}", j, j + len);

                                Buffer.BlockCopy(element.contents, j, blah, 0, len);
                                j += len;

                                var output = decoder.DecodeFromCodes(blah);
                                buffertje += output;

                                if (cleanData.Length < (cleanDataOffset + output.Length))
                                    Array.Resize<byte>(ref cleanData, cleanDataOffset + output.Length);

                                for (var k = 0; k < output.Length; k++)
                                    cleanData[cleanDataOffset++] = (byte)output[k];
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                nosave = true;
                                break;
                            }
                            //Console.WriteLine(output);
                            //File.AppendAllText(filename, output);
                            //break;
                        }

                        //if (nosave)
                        //    continue;


                        File.WriteAllText(Path.Combine("output", hur + "_text.txt"), buffertje);
                    }
                    else
                    {
                        cleanData = new byte[element.sizeInFile];
                        uint derp = (uint)(element.specialHash ^ 0xF9524287 | 1);

                        for (var j = 0; j < element.sizeInFile; j += 1024)
                        {
                            var blobsize = Math.Min(element.sizeInFile - j, 1024);

                            byte[] blah = new byte[1024];
                            Buffer.BlockCopy(element.contents, j, blah, 0, blobsize);

                            derp = Crypto(derp, blah);
                            Buffer.BlockCopy(blah, 0, cleanData, j, blobsize);
                        }

                    }

                    string ext = ".bin";

                    if (cleanData[0] == 0x89 && cleanData[1] == 0x50 && cleanData[2] == 0x4E && cleanData[3] == 0x47)
                        ext = ".png";
                    else if (cleanData[0] == 0x4F && cleanData[1] == 0x67 && cleanData[2] == 0x67)
                        ext = ".ogg";
                    else if (cleanData[0] == 0x42 && cleanData[1] == 0x4D && cleanData[2] == 0x46)
                        ext = ".bmf";

                    if (compressed)
                        ext = "_compressed" + ext;

                    var filename = Path.Combine("output", (uint)(element.elementId) + ext);
                    File.WriteAllBytes(filename, cleanData);
                }

                return elements;
            }
        }
    }
}
