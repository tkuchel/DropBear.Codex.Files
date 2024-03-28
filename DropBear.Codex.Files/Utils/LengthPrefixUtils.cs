namespace DropBear.Codex.Files.Utils;

/// <summary>
///     Utility class for working with length-prefixed byte streams.
/// </summary>
public static class LengthPrefixUtils
{
    /// <summary>
    ///     Writes bytes to a stream with a length prefix.
    /// </summary>
    /// <param name="stream">The stream to write to, must be writable.</param>
    /// <param name="bytes">The byte array to write.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream" /> or <paramref name="bytes" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the stream is not writable.</exception>
    /// <exception cref="IOException">Thrown if an error occurs while writing to the stream.</exception>
    public static void WriteLengthPrefixedBytes(Stream stream, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        ArgumentNullException.ThrowIfNull(bytes);

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
    ///     Reads bytes from a stream that were written with a length prefix.
    /// </summary>
    /// <param name="stream">The stream to read from, must be readable.</param>
    /// <returns>The byte array read from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the stream is not readable.</exception>
    /// <exception cref="IOException">Thrown if an error occurs while reading from the stream.</exception>
    /// <exception cref="EndOfStreamException">Thrown if the end of the stream is reached unexpectedly.</exception>
    public static byte[] ReadLengthPrefixedBytes(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));

        try
        {
            var length = ReadLengthPrefix(stream);
            var bytes = new byte[length];
            var totalBytesRead = 0;
            while (totalBytesRead < length)
            {
                var bytesRead = stream.Read(bytes, totalBytesRead, length - totalBytesRead);
                if (bytesRead == 0)
                    throw new EndOfStreamException(
                        "Unexpected end of stream encountered while reading length-prefixed bytes.");
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
            var prefixBytes = new byte[5]; // Maximum 5 bytes for a 32-bit int
            var numBytes = 0;
            do
            {
                var temp = (byte)(length & 0x7F);
                length >>= 7;
                if (length > 0) temp |= 0x80;
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
            var length = 0;
            var shift = 0;
            byte b;

            do
            {
                var byteRead = stream.ReadByte();
                if (byteRead == -1)
                    throw new EndOfStreamException("Unexpected end of stream encountered while reading length prefix.");

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
