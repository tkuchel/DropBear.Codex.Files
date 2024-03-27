using System;
using System.IO;

namespace DropBear.Codex.Files.Utils
{
    public static class LengthPrefixUtils
    {
        /// <summary>
        /// Writes bytes to a stream with a length prefix.
        /// </summary>
        /// <param name="stream">The stream to write to, must be writable.</param>
        /// <param name="bytes">The byte array to write.</param>
        public static void WriteLengthPrefixedBytes(Stream stream, byte[] bytes)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(stream));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            try
            {
                WriteLengthPrefix(stream, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                // Log or handle the error as appropriate for your application
                throw new IOException("Failed to write length-prefixed bytes to stream.", ex);
            }
        }

        /// <summary>
        /// Reads bytes from a stream that were written with a length prefix.
        /// </summary>
        /// <param name="stream">The stream to read from, must be readable.</param>
        /// <returns>The byte array read from the stream.</returns>
        public static byte[] ReadLengthPrefixedBytes(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            try
            {
                int length = ReadLengthPrefix(stream);
                byte[] bytes = new byte[length];
                int totalBytesRead = 0;
                while (totalBytesRead < length)
                {
                    int bytesRead = stream.Read(bytes, totalBytesRead, length - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        throw new EndOfStreamException("Unexpected end of stream encountered while reading length-prefixed bytes.");
                    }
                    totalBytesRead += bytesRead;
                }
                return bytes;
            }
            catch (Exception ex)
            {
                // Log or handle the error as appropriate for your application
                throw new IOException("Failed to read length-prefixed bytes from stream.", ex);
            }
        }

        private static void WriteLengthPrefix(Stream stream, int length)
        {
            try
            {
                byte[] prefixBytes = new byte[5]; // Maximum 5 bytes for a 32-bit int
                int numBytes = 0;
                do
                {
                    byte temp = (byte)(length & 0x7F);
                    length >>= 7;
                    if (length > 0)
                    {
                        temp |= 0x80;
                    }
                    prefixBytes[numBytes++] = temp;
                } while (length > 0);

                stream.Write(prefixBytes, 0, numBytes);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to write length prefix to stream.", ex);
            }
        }

        private static int ReadLengthPrefix(Stream stream)
        {
            try
            {
                int length = 0;
                int shift = 0;
                byte b;

                do
                {
                    int byteRead = stream.ReadByte();
                    if (byteRead == -1)
                    {
                        throw new EndOfStreamException("Unexpected end of stream encountered while reading length prefix.");
                    }

                    b = (byte)byteRead;
                    if (b == 0xFF)
                        throw new InvalidDataException("Invalid length prefix encoding.");

                    length |= (b & 0x7F) << shift;
                    shift += 7;
                } while ((b & 0x80) != 0);

                return length;
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read length prefix from stream.", ex);
            }
        }
    }
}