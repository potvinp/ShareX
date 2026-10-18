#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace ShareX.HelpersLib
{
    /// <summary>
    /// Per-user secret protection on macOS, standing in for Windows DPAPI: AES-256-GCM with a random key
    /// kept in the user's login keychain. Output layout: version (1) | nonce (12) | tag (16) | ciphertext.
    /// </summary>
    [SupportedOSPlatform("macos")]
    internal static class MacDataProtection
    {
        private const string KeychainService = "ShareX";
        private const string KeychainAccount = "SettingsEncryptionKey";
        private const byte FormatVersion = 1;
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int HeaderSize = 1 + NonceSize + TagSize;

        private static readonly Lazy<byte[]> key = new(LoadKey);

        public static byte[] Protect(byte[] data, byte[] optionalEntropy)
        {
            byte[] output = new byte[HeaderSize + data.Length];
            output[0] = FormatVersion;
            Span<byte> nonce = output.AsSpan(1, NonceSize);
            Span<byte> tag = output.AsSpan(1 + NonceSize, TagSize);
            RandomNumberGenerator.Fill(nonce);

            using AesGcm aes = new AesGcm(key.Value, TagSize);
            aes.Encrypt(nonce, data, output.AsSpan(HeaderSize), tag, optionalEntropy);
            return output;
        }

        public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy)
        {
            if (encryptedData.Length < HeaderSize || encryptedData[0] != FormatVersion)
            {
                throw new CryptographicException("The protected data is not in a supported format.");
            }

            byte[] output = new byte[encryptedData.Length - HeaderSize];
            using AesGcm aes = new AesGcm(key.Value, TagSize);
            aes.Decrypt(encryptedData.AsSpan(1, NonceSize), encryptedData.AsSpan(HeaderSize),
                encryptedData.AsSpan(1 + NonceSize, TagSize), output, optionalEntropy);
            return output;
        }

        private static byte[] LoadKey()
        {
            byte[] buffer = new byte[KeySize];
            int status = MacNativeMethods.KeychainGetOrCreateKey(KeychainService, KeychainAccount, buffer, buffer.Length);
            if (status != MacNativeMethods.StatusOk)
            {
                throw new CryptographicException($"Unable to read the settings encryption key from the keychain (status {status}).");
            }
            return buffer;
        }
    }
}
