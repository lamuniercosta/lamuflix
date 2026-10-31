using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LamuFlix.Infrastructure.Thumbnails;

internal sealed partial class WindowsJobObject : IDisposable
{
    private IntPtr handle;

    private WindowsJobObject(IntPtr handle)
    {
        this.handle = handle;
    }

    public static WindowsJobObject? TryAttach(Process process)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var handle = NativeMethods.CreateJobObject(IntPtr.Zero, IntPtr.Zero);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var job = new WindowsJobObject(handle);
        if (!NativeMethods.AssignProcessToJobObject(handle, process.Handle))
        {
            job.Dispose();
            return null;
        }

        return job;
    }

    public void Terminate()
    {
        if (handle != IntPtr.Zero)
        {
            _ = NativeMethods.TerminateJobObject(handle, 1);
        }
    }

    public void Dispose()
    {
        if (handle != IntPtr.Zero)
        {
            _ = NativeMethods.CloseHandle(handle);
            handle = IntPtr.Zero;
        }
    }

    private static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateJobObjectW")]
        internal static partial IntPtr CreateJobObject(IntPtr jobAttributes, IntPtr name);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TerminateJobObject(IntPtr job, uint exitCode);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr handle);
    }
}
