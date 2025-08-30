using System;
using System.IO;

namespace UpSkope
{
    public static class BinaryWriterExtensions
    {
        public static void WriteInt32BE(this BinaryWriter writer, Int32 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                writer.Write(bytes);
            }
            else
            {
                writer.Write(value);
            }
        }

        public static void WriteInt32LE(this BinaryWriter writer, Int32 value)
        {
            writer.Write(value);
        }

        public static void WriteInt16BE(this BinaryWriter writer, Int16 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                writer.Write(bytes);
            }
            else
            {
                writer.Write(value);
            }
        }

        public static void WriteInt16LE(this BinaryWriter writer, Int16 value)
        {
            writer.Write(value);
        }
    }
} 