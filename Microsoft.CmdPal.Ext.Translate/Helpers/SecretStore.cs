// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.CmdPal.Ext.Translate.Helpers;

/// <summary>
/// DPAPI-protected credential file (current user only). The key is never written to settings.json,
/// never to logs, and never sent anywhere except the configured translation endpoint.
/// </summary>
internal static class SecretStore
{
    private const uint CryptProtectUiForbidden = 0x1;
    private const string EnvironmentVariable = "CMDPAL_TRANSLATE_KEY";

    public static string LoadApiKey()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        try
        {
            var path = SettingsManager.CredentialPath();
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return Unprotect(File.ReadAllBytes(path)) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool SaveApiKey(string key)
    {
        var path = SettingsManager.CredentialPath();
        if (string.IsNullOrWhiteSpace(key))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return false;
        }

        var protectedBytes = Protect(key.Trim());
        File.WriteAllBytes(path, protectedBytes);
        return true;
    }

    private static byte[] Protect(string secret)
    {
        var plain = Marshal.StringToHGlobalUni(secret);
        var input = new DataBlob { cbData = (secret.Length + 1) * 2, pbData = plain };
        var output = default(DataBlob);
        try
        {
            if (!CryptProtectData(ref input, "CmdPal Translate", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, ref output))
            {
                throw new InvalidOperationException("CryptProtectData failed: " + Marshal.GetLastWin32Error());
            }

            var bytes = new byte[output.cbData];
            Marshal.Copy(output.pbData, bytes, 0, output.cbData);
            return bytes;
        }
        finally
        {
            if (output.pbData != IntPtr.Zero)
            {
                LocalFree(output.pbData);
            }

            Marshal.FreeHGlobal(plain);
        }
    }

    private static string? Unprotect(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        var input = new DataBlob { cbData = bytes.Length, pbData = pointer };
        var output = default(DataBlob);
        try
        {
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, ref output))
            {
                return null;
            }

            return Marshal.PtrToStringUni(output.pbData);
        }
        finally
        {
            if (output.pbData != IntPtr.Zero)
            {
                LocalFree(output.pbData);
            }

            Marshal.FreeHGlobal(pointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr handle);
}
