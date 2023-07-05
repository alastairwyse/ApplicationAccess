/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Compresses and encodes strings as Base64 (and performs equivalent decoding).
    /// </summary>
    public class Base64StringEncoder
    {
        /// <summary>
        /// Compresses and encodes a string as Base64.
        /// </summary>
        /// <param name="inputString">The string to encode.</param>
        /// <returns>The encoded string.</returns>
        public String Encode(String inputString)
        {
            using (var inputMemoryStream = new MemoryStream())
            using (var outputMemoryStream = new MemoryStream())
            using (var gZipStream = new GZipStream(outputMemoryStream, CompressionLevel.SmallestSize))
            {
                // Convert the string to a byte array and compress
                Byte[] inputStringAsByteArray = Encoding.UTF8.GetBytes(inputString);
                inputMemoryStream.Write(inputStringAsByteArray);
                inputMemoryStream.Position = 0;
                inputMemoryStream.CopyTo(gZipStream);
                gZipStream.Flush();
                Byte[] compressedBytes = new Byte[outputMemoryStream.Position];
                outputMemoryStream.Position = 0;
                outputMemoryStream.Read(compressedBytes, 0, compressedBytes.Length);
                // Convert the compressed bytes to Base64
                return Convert.ToBase64String(compressedBytes);
            }
        }

        /// <summary>
        /// Decocdes and decompresses a Base64 encoded string.
        /// </summary>
        /// <param name="encodedString">The string to decode.</param>
        /// <returns>The decoded string.</returns>
        public String Decode(String encodedString)
        {
            using (var inputMemoryStream = new MemoryStream())
            using (var outputMemoryStream = new MemoryStream())
            using (var gZipStream = new GZipStream(inputMemoryStream, CompressionMode.Decompress))
            {
                // Convert the string to a byte array and decompress
                Byte[] compressedBytes = Convert.FromBase64String(encodedString);
                inputMemoryStream.Write(compressedBytes, 0, compressedBytes.Length);
                inputMemoryStream.Position = 0;
                gZipStream.CopyTo(outputMemoryStream);
                gZipStream.Flush();
                Byte[] uncompressedBytes = new Byte[outputMemoryStream.Position];
                outputMemoryStream.Position = 0;
                outputMemoryStream.Read(uncompressedBytes, 0, uncompressedBytes.Length);
                // Convert the uncompressed bytes to a string
                return Encoding.UTF8.GetString(uncompressedBytes);
            }
        }
    }
}
