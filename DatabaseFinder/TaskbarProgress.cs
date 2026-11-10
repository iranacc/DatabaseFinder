namespace DatabaseFinder;

using System;
using System.Runtime.InteropServices;

public enum TaskbarProgressState
{
    NoProgress = 0,
    Indeterminate = 1,
    Normal = 2,
    Error = 4,
    Paused = 8
}

public static class TaskbarProgress
{
    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        [PreserveSig] void HrInit();
        [PreserveSig] void AddTab(IntPtr hwnd);
        [PreserveSig] void DeleteTab(IntPtr hwnd);
        [PreserveSig] void ActivateTab(IntPtr hwnd);
        [PreserveSig] void SetActiveAlt(IntPtr hwnd);
        [PreserveSig] void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        [PreserveSig] void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        [PreserveSig] void SetProgressState(IntPtr hwnd, TaskbarProgressState tbpFlags);
        [PreserveSig] void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        [PreserveSig] void UnregisterTab(IntPtr hwndTab);
        [PreserveSig] void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        [PreserveSig] void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        [PreserveSig] void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        [PreserveSig] void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        [PreserveSig] void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        [PreserveSig] void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
        [PreserveSig] void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
        [PreserveSig] void SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
    }

    [ComImport]
    [Guid("56f26429-b352-49e4-a20b-1f329372a0ba")]
    [ClassInterface(ClassInterfaceType.None)]
    private class TaskbarInstance { }

    private static ITaskbarList3? _taskbar;
    private static readonly bool _supported = Environment.OSVersion.Version >= new Version(6, 1);

    static TaskbarProgress()
    {
        if (!_supported) return;
        try
        {
            _taskbar = (ITaskbarList3)new TaskbarInstance();
            _taskbar.HrInit();
        }
        catch
        {
            _taskbar = null;
        }
    }

    public static void SetState(IntPtr windowHandle, TaskbarProgressState state)
    {
        if (_taskbar == null || windowHandle == IntPtr.Zero) return;
        try
        {
            _taskbar.SetProgressState(windowHandle, state);
        }
        catch { }
    }

    public static void SetValue(IntPtr windowHandle, ulong current, ulong total)
    {
        if (_taskbar == null || windowHandle == IntPtr.Zero) return;
        try
        {
            _taskbar.SetProgressValue(windowHandle, current, total);
        }
        catch { }
    }
}
