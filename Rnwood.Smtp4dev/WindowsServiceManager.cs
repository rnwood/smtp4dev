using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Rnwood.Smtp4dev
{
    [SupportedOSPlatform("windows")]
    internal static class WindowsServiceManager
    {
        private const string ServiceName = "Smtp4dev";
        private const string ServiceDisplayName = "smtp4dev";
        private const string ServiceDescriptionText = "Fake SMTP email server for development and testing.";

        // Access rights for OpenSCManager
        private const int SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const int SC_MANAGER_CONNECT = 0x0001;

        // Service start type
        private const int SERVICE_AUTO_START = 0x00000002;

        // Service type
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

        // Service error control
        private const int SERVICE_ERROR_NORMAL = 0x00000001;

        // Access rights for OpenService
        private const int SERVICE_ALL_ACCESS = 0xF01FF;
        private const int DELETE = 0x00010000;

        // Service controls accepted
        private const int SERVICE_ACCEPT_STOP = 0x00000001;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, int desiredAccess);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            int dwDesiredAccess,
            int dwServiceType,
            int dwStartType,
            int dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(IntPtr hService);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfig2(IntPtr hService, int dwInfoLevel, ref ServiceDescription lpInfo);

        private const int SERVICE_CONFIG_DESCRIPTION = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ServiceDescription
        {
            public string lpDescription;
        }

        /// <summary>
        /// Installs smtp4dev as a Windows service. Requires administrator privileges.
        /// </summary>
        /// <param name="binaryPath">The full path to the executable (without --service flag).</param>
        public static void Install(string binaryPath)
        {
            string binPathWithFlag = $"\"{binaryPath}\" --service";

            IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
            if (scm == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager. Make sure you are running as Administrator.");
            }

            try
            {
                IntPtr svc = CreateService(
                    scm,
                    ServiceName,
                    ServiceDisplayName,
                    SERVICE_ALL_ACCESS,
                    SERVICE_WIN32_OWN_PROCESS,
                    SERVICE_AUTO_START,
                    SERVICE_ERROR_NORMAL,
                    binPathWithFlag,
                    null,
                    IntPtr.Zero,
                    null,
                    null,
                    null);

                if (svc == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to create service '{ServiceName}'.");
                }

                try
                {
                    var description = new ServiceDescription { lpDescription = ServiceDescriptionText };
                    // Non-critical: failure here means the service installs without a description
                    if (!ChangeServiceConfig2(svc, SERVICE_CONFIG_DESCRIPTION, ref description))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Installed service '{ServiceName}' but could not set the service description.");
                    }
                }
                finally
                {
                    CloseServiceHandle(svc);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Uninstalls the smtp4dev Windows service. Requires administrator privileges.
        /// </summary>
        public static void Uninstall()
        {
            IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager. Make sure you are running as Administrator.");
            }

            try
            {
                IntPtr svc = OpenService(scm, ServiceName, DELETE);
                if (svc == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 1060) // ERROR_SERVICE_DOES_NOT_EXIST
                    {
                        throw new InvalidOperationException($"Service '{ServiceName}' is not installed.");
                    }
                    throw new Win32Exception(error, $"Failed to open service '{ServiceName}'.");
                }

                try
                {
                    if (!DeleteService(svc))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to delete service '{ServiceName}'.");
                    }
                }
                finally
                {
                    CloseServiceHandle(svc);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }
    }
}
