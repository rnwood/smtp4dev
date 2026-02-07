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
        
        // Timeout constants for Docker operations
        private const int DockerPortQueryTimeoutMs = 5000;
        private const int DockerPortQueryRetries = 5;  // Number of retry attempts for port queries
        private const int DockerPortQueryRetryDelayMs = 1000;  // Delay between retries
        private const int DockerStopTimeoutMs = 10000;
        private const int DockerRemoveTimeoutMs = 5000;

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
        /// Creates an SMTP client with protocol logging enabled.
        /// Logs all SMTP protocol communication to test output for debugging.
        /// </summary>
        protected MailKit.Net.Smtp.SmtpClient CreateSmtpClientWithLogging()
        {
            var client = new MailKit.Net.Smtp.SmtpClient(new TestOutputProtocolLogger(output, "SMTP"));
            output.WriteLine($"  [SMTP] Created SmtpClient with protocol logging enabled");
            return client;
        }

        /// <summary>
        /// Creates an IMAP client with protocol logging enabled.
        /// Logs all IMAP protocol communication to test output for debugging.
        /// </summary>
        protected MailKit.Net.Imap.ImapClient CreateImapClientWithLogging()
        {
            var client = new MailKit.Net.Imap.ImapClient(new TestOutputProtocolLogger(output, "IMAP"));
            output.WriteLine($"  [IMAP] Created ImapClient with protocol logging enabled");
            return client;
        }

        /// <summary>
        /// Creates a POP3 client with protocol logging enabled.
        /// Logs all POP3 protocol communication to test output for debugging.
        /// </summary>
        protected MailKit.Net.Pop3.Pop3Client CreatePop3ClientWithLogging()
        {
            var client = new MailKit.Net.Pop3.Pop3Client(new TestOutputProtocolLogger(output, "POP3"));
            output.WriteLine($"  [POP3] Created Pop3Client with protocol logging enabled");
            return client;
        }

        /// <summary>
        /// Query Docker for the host port mapped to a container's internal port.
        /// Uses `docker port <container> <internal_port>` command.
        /// Includes retry logic to handle race conditions on slow systems (e.g., ARM64 with QEMU emulation).
        /// </summary>
        private int? GetDockerHostPort(string containerName, int internalPort, ITestOutputHelper output)
        {
            // Retry logic for ARM64/QEMU environments where port mappings may not be immediately available
            for (int attempt = 1; attempt <= DockerPortQueryRetries; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        output.WriteLine($"    [Docker] Retry attempt {attempt}/{DockerPortQueryRetries} after {DockerPortQueryRetryDelayMs}ms delay");
                        System.Threading.Thread.Sleep(DockerPortQueryRetryDelayMs);
                    }
                    
                    output.WriteLine($"    [Docker] Executing: docker port {containerName} {internalPort}");
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
                    if (process == null)
                    {
                        output.WriteLine($"    [Docker] ERROR: Failed to start docker process");
                        if (attempt < DockerPortQueryRetries) continue;
                        return null;
                    }

                    string portOutput = process.StandardOutput.ReadToEnd();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit(DockerPortQueryTimeoutMs);

                    if (process.ExitCode != 0)
                    {
                        output.WriteLine($"    [Docker] ERROR: docker port command failed for {containerName}:{internalPort}");
                        output.WriteLine($"    [Docker] Exit code: {process.ExitCode}");
                        if (!string.IsNullOrWhiteSpace(errorOutput))
                        {
                            output.WriteLine($"    [Docker] Error output: {errorOutput}");
                        }
                        if (attempt < DockerPortQueryRetries) continue;
                        return null;
                    }

                    // Output format is like: "0.0.0.0:32768" or "[::]:32768" or "0.0.0.0:32768\n[::]:32768"
                    // We need to extract the port number - when both IPv4 and IPv6 mappings exist,
                    // Docker assigns the same host port for both, so we just extract the first one.
                    var match = Regex.Match(portOutput, @":(\d+)");
                    if (match.Success)
                    {
                        int hostPort = int.Parse(match.Groups[1].Value);
                        output.WriteLine($"    [Docker] Port mapping: {containerName}:{internalPort} -> host:{hostPort} (attempt {attempt})");
                        return hostPort;
                    }

                    output.WriteLine($"    [Docker] ERROR: Could not parse docker port output: '{portOutput}'");
                    if (attempt < DockerPortQueryRetries) continue;
                    return null;
                }
                catch (Exception ex)
                {
                    output.WriteLine($"    [Docker] ERROR: Exception querying docker port (attempt {attempt}): {ex.Message}");
                    if (attempt < DockerPortQueryRetries)
                    {
                        output.WriteLine($"    [Docker] Will retry...");
                        continue;
                    }
                    output.WriteLine($"    [Docker] Stack trace: {ex.StackTrace}");
                    return null;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Stop and remove a Docker container by name.
        /// </summary>
        private void CleanupDockerContainer(string containerName, ITestOutputHelper output)
        {
            try
            {
                output.WriteLine($"    [Docker] Cleaning up Docker container: {containerName}");
                
                // Stop the container
                output.WriteLine($"    [Docker] Executing: docker stop {containerName}");
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
                    stopProcess?.WaitForExit(DockerStopTimeoutMs);
                    if (stopProcess?.ExitCode != 0)
                    {
                        var stderr = stopProcess.StandardError.ReadToEnd();
                        output.WriteLine($"    [Docker] docker stop exited with code {stopProcess.ExitCode}. Error: {stderr}");
                    }
                }

                // Remove the container
                output.WriteLine($"    [Docker] Executing: docker rm -f {containerName}");
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
                    rmProcess?.WaitForExit(DockerRemoveTimeoutMs);
                    if (rmProcess?.ExitCode != 0)
                    {
                        var stderr = rmProcess.StandardError.ReadToEnd();
                        output.WriteLine($"    [Docker] docker rm exited with code {rmProcess.ExitCode}. Error: {stderr}");
                    }
                }

                output.WriteLine($"    [Docker] Docker container {containerName} cleaned up");
            }
            catch (Exception ex)
            {
                output.WriteLine($"    [Docker] ERROR: Exception cleaning up docker container: {ex.Message}");
                output.WriteLine($"    [Docker] Stack trace: {ex.StackTrace}");
            }
        }

        protected void RunE2ETest(Action<E2ETestContext> test, E2ETestOptions options = null)
        {
            var testStartTime = DateTime.UtcNow;
            output.WriteLine($"[{testStartTime:HH:mm:ss.fff}] E2E Test Started");
            
            options ??= new E2ETestOptions();

            string workingDir = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_WORKINGDIR");
            string binary = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");
            bool useDefaultDBPath = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_USEDEFAULTDBPATH") == "1";
            List<string> args = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_ARGS")?.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                ?.ToList() ?? new List<string>();
            
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Environment Variables:");
            output.WriteLine($"  SMTP4DEV_E2E_WORKINGDIR: {workingDir ?? "(not set)"}");
            output.WriteLine($"  SMTP4DEV_E2E_BINARY: {binary ?? "(not set)"}");
            output.WriteLine($"  SMTP4DEV_E2E_USEDEFAULTDBPATH: {useDefaultDBPath}");
            output.WriteLine($"  SMTP4DEV_E2E_ARGS count: {args.Count}");

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
            
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Running in {(isDockerMode ? "Docker" : "Direct")} mode");
            
            // Generate unique container name for Docker mode to avoid conflicts
            // Format: smtp4dev-e2e-<first 12 chars of GUID> (total 24 chars, well within Docker's 64 char limit)
            string dockerContainerName = null;
            if (isDockerMode)
            {
                dockerContainerName = $"smtp4dev-e2e-{Guid.NewGuid():N}".Substring(0, 24);
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Docker container name: {dockerContainerName}");
                
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

            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Binary: {binary}");
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Args: " + string.Join(" ", args.Select(a => $"\"{a}\"")));

            var processStartTime = DateTime.UtcNow;
            output.WriteLine($"[{processStartTime:HH:mm:ss.fff}] Starting server process...");
            
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

                    output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Waiting for server to start (timeout: 180s)...");
                    int lineCount = 0;
                    var startupParseStartTime = DateTime.UtcNow;

                    while ((baseUrl == null || !smtpPortNumber.HasValue || !imapPortNumber.HasValue || !pop3PortNumber.HasValue) && serverOutput.MoveNext())
                    {
                        string newLine = serverOutput.Current;
                        lineCount++;
                        output.WriteLine(newLine);

                        if (newLine.Contains("Now listening on: http://"))
                        {
                            var httpListenTime = DateTime.UtcNow;
                            output.WriteLine($"[{httpListenTime:HH:mm:ss.fff}] HTTP server started (after {(httpListenTime - processStartTime).TotalSeconds:F2}s)");
                            
                            // Handle both IPv4 (http://localhost:5000) and IPv6 (http://[::]:80) formats
                            int internalPortNumber = int.Parse(Regex.Replace(newLine, @".*http://[^\s]+:(\d+)", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            int portNumber;
                            if (isDockerMode && dockerContainerName != null)
                            {
                                var portQueryStart = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryStart:HH:mm:ss.fff}] Querying Docker port mapping for HTTP port {internalPortNumber}...");
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalPortNumber, output);
                                var portQueryEnd = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryEnd:HH:mm:ss.fff}] Docker port query completed in {(portQueryEnd - portQueryStart).TotalMilliseconds:F0}ms (mapped to {mappedPort})");
                                portNumber = mappedPort ?? internalPortNumber;
                            }
                            else
                            {
                                portNumber = internalPortNumber;
                            }
                            baseUrl = new Uri($"http://localhost:{portNumber}{options.TestPath ?? options.BasePath ?? ""}");
                            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Base URL set to: {baseUrl}");
                        }

                        if (newLine.Contains("SMTP Server is listening on port"))
                        {
                            var smtpListenTime = DateTime.UtcNow;
                            output.WriteLine($"[{smtpListenTime:HH:mm:ss.fff}] SMTP server started (after {(smtpListenTime - processStartTime).TotalSeconds:F2}s)");
                            
                            int internalSmtpPort = int.Parse(Regex.Replace(newLine, @".*SMTP Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                var portQueryStart = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryStart:HH:mm:ss.fff}] Querying Docker port mapping for SMTP port {internalSmtpPort}...");
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalSmtpPort, output);
                                var portQueryEnd = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryEnd:HH:mm:ss.fff}] Docker port query completed in {(portQueryEnd - portQueryStart).TotalMilliseconds:F0}ms (mapped to {mappedPort})");
                                smtpPortNumber = mappedPort ?? internalSmtpPort;
                            }
                            else
                            {
                                smtpPortNumber = internalSmtpPort;
                            }
                            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] SMTP port: {smtpPortNumber}");
                        }

                        if (newLine.Contains("IMAP Server is listening on port"))
                        {
                            var imapListenTime = DateTime.UtcNow;
                            output.WriteLine($"[{imapListenTime:HH:mm:ss.fff}] IMAP server started (after {(imapListenTime - processStartTime).TotalSeconds:F2}s)");
                            
                            int internalImapPort = int.Parse(Regex.Replace(newLine, @".*IMAP Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                var portQueryStart = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryStart:HH:mm:ss.fff}] Querying Docker port mapping for IMAP port {internalImapPort}...");
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalImapPort, output);
                                var portQueryEnd = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryEnd:HH:mm:ss.fff}] Docker port query completed in {(portQueryEnd - portQueryStart).TotalMilliseconds:F0}ms (mapped to {mappedPort})");
                                imapPortNumber = mappedPort ?? internalImapPort;
                            }
                            else
                            {
                                imapPortNumber = internalImapPort;
                            }
                            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] IMAP port: {imapPortNumber}");
                        }

                        if (newLine.Contains("POP3 Server is listening on port"))
                        {
                            var pop3ListenTime = DateTime.UtcNow;
                            output.WriteLine($"[{pop3ListenTime:HH:mm:ss.fff}] POP3 server started (after {(pop3ListenTime - processStartTime).TotalSeconds:F2}s)");
                            
                            int internalPop3Port = int.Parse(Regex.Replace(newLine, @".*POP3 Server is listening on port (\d+).*", "$1"));
                            
                            // For Docker, query the actual mapped host port dynamically
                            if (isDockerMode && dockerContainerName != null)
                            {
                                var portQueryStart = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryStart:HH:mm:ss.fff}] Querying Docker port mapping for POP3 port {internalPop3Port}...");
                                int? mappedPort = GetDockerHostPort(dockerContainerName, internalPop3Port, output);
                                var portQueryEnd = DateTime.UtcNow;
                                output.WriteLine($"[{portQueryEnd:HH:mm:ss.fff}] Docker port query completed in {(portQueryEnd - portQueryStart).TotalMilliseconds:F0}ms (mapped to {mappedPort})");
                                pop3PortNumber = mappedPort ?? internalPop3Port;
                            }
                            else
                            {
                                pop3PortNumber = internalPop3Port;
                            }
                            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] POP3 port: {pop3PortNumber}");
                            
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
                            var failTime = DateTime.UtcNow;
                            output.WriteLine($"[{failTime:HH:mm:ss.fff}] ERROR: Startup completed but did not catch all variables (after {(failTime - processStartTime).TotalSeconds:F2}s, {lineCount} lines)");
                            throw new Exception($@"Startup completed but did not catch variables from startup output:
                            baseUrl:{baseUrl}
                            smtpPortNumber: {smtpPortNumber}
                            imapPortNumber: {imapPortNumber}
                            pop3PortNumber: {pop3PortNumber}");
                        }
                    }

                    var startupCompleteTime = DateTime.UtcNow;
                    output.WriteLine($"[{startupCompleteTime:HH:mm:ss.fff}] Server startup complete! Total time: {(startupCompleteTime - processStartTime).TotalSeconds:F2}s, parsed {lineCount} lines");
                    output.WriteLine($"[{startupCompleteTime:HH:mm:ss.fff}] Configuration: HTTP={baseUrl}, SMTP={smtpPortNumber}, IMAP={imapPortNumber}, POP3={pop3PortNumber}@{pop3Host}");


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

                    var testExecutionStartTime = DateTime.UtcNow;
                    output.WriteLine($"[{testExecutionStartTime:HH:mm:ss.fff}] Starting test execution...");
                    output.WriteLine($"[{testExecutionStartTime:HH:mm:ss.fff}] Using Pop3Host: {pop3Host}, Pop3Port: {pop3PortNumber}");
                    
                    test(new E2ETestContext
                    {
                        BaseUrl = baseUrl,
                        SmtpPortNumber = smtpPortNumber.Value,
                        ImapPortNumber = imapPortNumber.Value,
                        Pop3PortNumber = pop3PortNumber.Value,
                        Pop3Host = pop3Host
                    });
                    
                    var testCompleteTime = DateTime.UtcNow;
                    output.WriteLine($"[{testCompleteTime:HH:mm:ss.fff}] Test execution complete! Test duration: {(testCompleteTime - testExecutionStartTime).TotalSeconds:F2}s");
                }
                finally
                {
                    var cleanupStartTime = DateTime.UtcNow;
                    output.WriteLine($"[{cleanupStartTime:HH:mm:ss.fff}] Starting cleanup...");
                    
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
                    
                    var cleanupCompleteTime = DateTime.UtcNow;
                    var totalTestTime = cleanupCompleteTime - testStartTime;
                    output.WriteLine($"[{cleanupCompleteTime:HH:mm:ss.fff}] Cleanup complete. Total E2E test duration: {totalTestTime.TotalSeconds:F2}s");
                }
            }
        }
    }
}