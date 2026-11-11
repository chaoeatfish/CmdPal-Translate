// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CmdPal.Ext.Translate.Helpers;

/// <summary>Explicit, user-invoked clipboard read. The extension never touches the clipboard on startup.</summary>
internal static class ClipboardText
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public static string? TryRead()
    {
        string? result = null;
        var thread = new Thread(() => result = ReadCore()) { IsBackground = true };
        try
        {
            thread.SetApartmentState(ApartmentState.STA);
        }
        catch (PlatformNotSupportedException)
        {
            return ReadCore();
        }

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(2));
        return result;
    }

    public static bool TryWrite(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var succeeded = false;
        var thread = new Thread(() => succeeded = WriteCore(text)) { IsBackground = true };
        try
        {
            thread.SetApartmentState(ApartmentState.STA);
        }
        catch (PlatformNotSupportedException)
        {
            return WriteCore(text);
        }

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(2));
        return succeeded;
    }

    private static bool WriteCore(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return false;
        }

        try
        {
            EmptyClipboard();
            var size = (text.Length + 1) * 2;
            var handle = GlobalAlloc(GmemMoveable, new UIntPtr((ulong)size));
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var locked = GlobalLock(handle);
            if (locked == IntPtr.Zero)
            {
                GlobalFree(handle);
                return false;
            }

            Marshal.Copy(text.ToCharArray(), 0, locked, text.Length);
            Marshal.WriteInt16(locked, text.Length * 2, 0);
            GlobalUnlock(handle);

            if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
            {
                GlobalFree(handle);
                return false;
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string? ReadCore()
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return null;
        }

        try
        {
            var handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var locked = GlobalLock(handle);
            if (locked == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(locked);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr handle);
}
