using Medallion.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests
    {
        protected readonly ITestOutputHelper output;

        public E2ETests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public class E2ETestOptions
        {
            public bool InMemoryDB { get; set; }
            public string BasePath { get; set; }
            
            public string TestPath { get; set; }
            public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        }

        public class E2ETestContext
        {
            public Uri BaseUrl { get; set; }
            public int SmtpPortNumber { get; set; }

            public int ImapPortNumber { get; set; }
            public int Pop3PortNumber { get; set; }
            public string Pop3Host { get; set; } = "localhost";
        }

        /// <summary>
        /// Query Docker for the host port mapped to a container's internal port.
        /// Uses `docker port <container> <internal_port>` command.
        /// </summary>
        private int? GetDockerHostPort(string containerName, int internalPort, ITestOutputHelper output)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"port {containerName} {internalPort}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return null;

                string portOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                if (process.ExitCode != 0)
                {
                    output.WriteLine($"docker port command failed for {containerName}:{internalPort}");
                    return null;
                }

                // Output format is like: "0.0.0.0:32768" or "[::]:32768" or "0.0.0.0:32768\n[::]:32768"
                // We need to extract the port number
                var match = Regex.Match(portOutput, @":(\d+)");
                if (match.Success)
                {
                    int hostPort = int.Parse(match.Groups[1].Value);
                    output.WriteLine($"Docker port mapping: {containerName}:{internalPort} -> host:{hostPort}");
                    return hostPort;
                }

                output.WriteLine($"Could not parse docker port output: {portOutput}");
                return null;
            }
            catch (Exception ex)
            {
                output.WriteLine($"Error querying docker port: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stop and remove a Docker container by name.
        /// </summary>
        private void CleanupDockerContainer(string containerName, ITestOutputHelper output)
        {
            try
            {
                output.WriteLine($"Cleaning up Docker container: {containerName}");
                
                // Stop the container
                var stopInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"stop {containerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var stopProcess = Process.Start(stopInfo))
                {
                    stopProcess?.WaitForExit(10000);
                }

                // Remove the container
                var rmInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {containerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var rmProcess = Process.Start(rmInfo))
                {
                    rmProcess?.WaitForExit(5000);
                }

                output.WriteLine($"Docker container {containerName} cleaned up");
            }
            catch (Exception ex)
            {
                output.WriteLine($"Error cleaning up docker container: {ex.Message}");
            }
        }

        protected void RunE2ETest(Action<E2ETestContext> test, E2ETestOptions options = null)
        {
            options ??= new E2ETestOptions();

            string workingDir = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_WORKINGDIR");
            string binary = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");
            bool useDefaultDBPath = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_USEDEFAULTDBPATH") == "1";
            List<string> args = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_ARGS")?.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                ?.ToList() ?? new List<string>();

            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetFullPath("../../../../Rnwood.Smtp4dev");
            }
            else
            {
                workingDir = Path.GetFullPath(workingDir);
            }

            if (string.IsNullOrEmpty(binary))
            {
                binary = "dotnet";

                //.NETCoreapp,Version=v3.1
                string framework = typeof(Program)
                    .Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>()?
                    .FrameworkName;

                //netcoreapp3.1
                string folder = framework.TrimStart('.').Replace("CoreApp,Version=v", "").ToLower();

                // Determine build configuration - prefer Release if it exists, fallback to Debug
                string configuration = "Debug";
                string releaseModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev/bin/Release/{folder}/Rnwood.Smtp4dev.dll");
                string debugModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev/bin/Debug/{folder}/Rnwood.Smtp4dev.dll");
                
                if (File.Exists(releaseModule))
                {
                    configuration = "Release";
                }
                else if (!File.Exists(debugModule))
                {
                    // If neither exists, check if we're in a CI environment and provide a helpful error
                    throw new FileNotFoundException($"Could not find Rnwood.Smtp4dev.dll in either Release ({releaseModule}) or Debug ({debugModule}) configurations. Ensure the main project is built before running E2E tests.");
                }

                string mainModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev/bin/{configuration}/{folder}/Rnwood.Smtp4dev.dll");
                args.Insert(0, mainModule);

            }

            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180)).Token; // Increased from 120s to 180s for CI environments
            string dbPath = Path.GetTempFileName();
            File.Delete(dbPath);

            // Only add port arguments if not running in Docker mode (binary != "docker")
            // In Docker mode, port mappings are controlled via docker -p flags and args from SMTP4DEV_E2E_ARGS
            bool isDockerMode = binary == "docker";
            
            // Generate unique container name for Docker mode to avoid conflicts
            string dockerContainerName = null;
            if (isDockerMode)
            {
                dockerContainerName = $"smtp4dev-e2e-{Guid.NewGuid():N}".Substring(0, 32);
                output.WriteLine($"Docker container name: {dockerContainerName}");
                
                // Insert --name argument after 'run' and before other args
                // Find the index of 'run' in args and insert --name after it
                int runIndex = args.IndexOf("run");
                if (runIndex >= 0)
                {
                    args.Insert(runIndex + 1, "--name");
                    args.Insert(runIndex + 2, dockerContainerName);
                }
            }

            args.AddRange(new[] {
                options.InMemoryDB ? "--db=" : useDefaultDBPath ? "" : $"--db={dbPath}", "--nousersettings",
                "--tlsmode=StartTls"
            }.Where(a => a != ""));

            if (!args.Any(a => a.StartsWith("--urls")) && (!options?.EnvironmentVariables.ContainsKey("SERVEROPTIONS__URLS") ?? true))
            {
                args.Add("--urls=http://*:0");
            }

            // If SERVEROPTIONS__URLS is set, remove any existing --urls arguments to allow the env var to take precedence
            if (options?.EnvironmentVariables.ContainsKey("SERVEROPTIONS__URLS") ?? false)
            {
                args.RemoveAll(a => a.StartsWith("--urls"));
            }

            if (!isDockerMode && !args.Any(a => a.StartsWith("--imapport")))
            {
                args.Add("--imapport=0");
            }

            if (!isDockerMode && !args.Any(a => a.StartsWith("--pop3port")))
            {
                args.Add("--pop3port=0");
            }
            
            if (!isDockerMode && !args.Any(a => a.StartsWith("--smtpport")))
            {
                args.Add("--smtpport=0");
            }

            if (!args.Any(a => a.StartsWith("--hostname")))
            {
                args.Add("--hostname=localhost");
            }



            if (!string.IsNullOrEmpty(options.BasePath))
            {
                args.Add($"--basepath={options.BasePath}");
            }
            else
            {
                args.Add("--basepath=");
            }

            output.WriteLine("Args: " + string.Join(" ", args.Select(a => $"\"{a}\"")));

            using (Command serverProcess = Command.Run(binary, args,
                       o => o.DisposeOnExit(false).WorkingDirectory(workingDir).EnvironmentVariables(options?.EnvironmentVariables ?? new Dictionary<string, string>()).CancellationToken(timeout)))
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = cancellationTokenSource.Token;

                try
                {
                    IEnumerator<string> serverOutput = serverProcess.GetOutputAndErrorLines().GetEnumerator();

                    Uri baseUrl = null;
                    int? smtpPortNumber = null;
                    int? imapPortNumber = null;
                    int? pop3PortNumber = null;
                    string pop3Host = "localhost";


                    while ((baseUrl == null || !smtpPortNumber.HasValue || !imapPortNumber.HasValue || !pop3PortNumber.HasValue) && serverOutput.MoveNext())
                    {
                        string newLine = serverOutput.Current;
                        output.WriteLine(newLine);

                        if (newLine.Contains("Now listening on: http://"))
                        {
                            // Handle both IPv4 (http://localhost:5000) and IPv6 (http://[::]:80) formats
                            int internalPortNumber = int.Parse(Regex.Replace(newLine, @".*http://[^\s]+:(\d+)", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            int portNumber;
                            if (isDockerMode && dockerContainerName != null)
                            {
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalPortNumber, output);
                                portNumber = mappedPort ?? internalPortNumber;
                            }
                            else
                            {
                                portNumber = internalPortNumber;
                            }
                            baseUrl = new Uri($"http://localhost:{portNumber}{options.TestPath ?? options.BasePath ?? ""}");
                        }

                        if (newLine.Contains("SMTP Server is listening on port"))
                        {
                            int internalSmtpPort = int.Parse(Regex.Replace(newLine, @".*SMTP Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalSmtpPort, output);
                                smtpPortNumber = mappedPort ?? internalSmtpPort;
                            }
                            else
                            {
                                smtpPortNumber = internalSmtpPort;
                            }
                        }

                        if (newLine.Contains("IMAP Server is listening on port"))
                        {
                            int internalImapPort = int.Parse(Regex.Replace(newLine, @".*IMAP Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalImapPort, output);
                                imapPortNumber = mappedPort ?? internalImapPort;
                            }
                            else
                            {
                                imapPortNumber = internalImapPort;
                            }
                        }

                        if (newLine.Contains("POP3 Server is listening on port"))
                        {
                            int internalPop3Port = int.Parse(Regex.Replace(newLine, @".*POP3 Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalPop3Port, output);
                                pop3PortNumber = mappedPort ?? internalPop3Port;
                            }
                            else
                            {
                                pop3PortNumber = internalPop3Port;
                            }
                            
                            // Try to parse the address from the same line (e.g. "POP3 Server is listening on port 53333 (::)")
                            var m = Regex.Match(newLine, @"POP3 Server is listening on port \d+ \(([^)]+)\)");
                            if (m.Success)
                            {
                                var addr = m.Groups[1].Value;
                                if (addr == "::" || addr == "::1" || addr.Contains(':'))
                                {
                                    pop3Host = "::1";
                                }
                                else if (addr == "0.0.0.0")
                                {
                                    pop3Host = "127.0.0.1";
                                }
                                else
                                {
                                    pop3Host = addr;
                                }
                            }
                        }

                        if (newLine.Contains("Application started. Press Ctrl+C to shut down."))
                        {
                            throw new Exception($@"Startup completed but did not catch variables from startup output:
                            baseUrl:{baseUrl}
                            smtpPortNumber: {smtpPortNumber}
                            imapPortNumber: {imapPortNumber}");
                        }
                    }


                    var task = Task.Run(() =>
                    {
                        while (serverOutput.MoveNext())
                        {
                            var newLine = serverOutput.Current;
                            output.WriteLine(newLine);
                        }

                        return Task.CompletedTask;
                    }, token);

                    Assert.False(serverProcess.Process.HasExited, "Server process failed");


                    output.WriteLine($"Using Pop3Host: {pop3Host}, Pop3Port: {pop3PortNumber}");
                    test(new E2ETestContext
                    {
                        BaseUrl = baseUrl,
                        SmtpPortNumber = smtpPortNumber.Value,
                        ImapPortNumber = imapPortNumber.Value,
                        Pop3PortNumber = pop3PortNumber.Value,
                        Pop3Host = pop3Host
                    });
                }
                finally
                {
                    // For Docker mode, properly stop and remove the container
                    if (isDockerMode && dockerContainerName != null)
                    {
                        CleanupDockerContainer(dockerContainerName, output);
                    }
                    else
                    {
                        serverProcess.TrySignalAsync(CommandSignal.ControlC).Wait();
                        serverProcess.StandardInput.Close();
                        if (!serverProcess.Process.WaitForExit(5000))
                        {
                            serverProcess.Kill();
                            output.WriteLine("E2E process didn't exit!");
                        }
                    }
  
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}