using Medallion.Shell;
using System;
using System.Collections.Generic;
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
    [Collection("E2E")]
    public class E2ETests
    {
        private readonly ITestOutputHelper output;

        public E2ETests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public class E2ETestOptions
        {
            public bool InMemoryDB { get; set; }
            public string BasePath { get; set; }
        }

        public class E2ETestContext
        {
            public Uri BaseUrl { get; set; }
            public int SmtpPortNumber { get; set; }

            public int ImapPortNumber { get; set; }
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

                string mainModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev/bin/Debug/{folder}/Rnwood.Smtp4dev.dll");
                args.Insert(0, mainModule);

            }

            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
            string dbPath = Path.GetTempFileName();
            File.Delete(dbPath);

            args.AddRange(new[] {
                "--debugsettings", options.InMemoryDB ? "--db=" : useDefaultDBPath ? "" : $"--db={dbPath}", "--nousersettings",
                "--tlsmode=StartTls"
            }.Where(a => a != ""));

            if (!args.Any(a => a.StartsWith("--urls")))
            {
                args.Add("--urls=http://*:0");
            }

            if (!args.Any(a => a.StartsWith("--imapport")))
            {
                args.Add("--imapport=0");
            }

            if (!args.Any(a => a.StartsWith("--smtpport")))
            {
                args.Add("--smtpport=0");
            }

            if (!args.Any(a => a.StartsWith("--smtpport")))
            {
                args.Add("--smtpport=0");
            }



            if (!string.IsNullOrEmpty(options.BasePath))
            {
                args.Add($"--basepath={options.BasePath}");
            }

            output.WriteLine("Args: " + string.Join(" ", args.Select(a => $"\"{a}\"")));

            using (Command serverProcess = Command.Run(binary, args,
                       o => o.DisposeOnExit(false).WorkingDirectory(workingDir).CancellationToken(timeout)))
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = cancellationTokenSource.Token;

                try
                {
                    IEnumerator<string> serverOutput = serverProcess.GetOutputAndErrorLines().GetEnumerator();

                    Uri baseUrl = null;
                    int? smtpPortNumber = null;
                    int? imapPortNumber = null;


                    while ((baseUrl == null || !smtpPortNumber.HasValue || !imapPortNumber.HasValue) && serverOutput.MoveNext())
                    {
                        string newLine = serverOutput.Current;
                        output.WriteLine(newLine);

                        if (newLine.StartsWith("Now listening on: http://"))
                        {
                            int portNumber = int.Parse(Regex.Replace(newLine, @".*http://[^\s]+:(\d+)", "$1"));
                            baseUrl = new Uri($"http://localhost:{portNumber}{options.BasePath ?? ""}");
                        }

                        if (newLine.StartsWith("SMTP Server is listening on port"))
                        {
                            smtpPortNumber = int.Parse(Regex.Replace(newLine, @"SMTP Server is listening on port (\d+).*", "$1"));
                        }

                        if (newLine.StartsWith("IMAP Server is listening on port"))
                        {
                            imapPortNumber = int.Parse(Regex.Replace(newLine, @"IMAP Server is listening on port (\d+).*", "$1"));
                        }

                        if (newLine.StartsWith("Application started. Press Ctrl+C to shut down."))
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


                    test(new E2ETestContext
                    {
                        BaseUrl = baseUrl,
                        SmtpPortNumber = smtpPortNumber.Value,
                        ImapPortNumber = imapPortNumber.Value
                    });
                }
                finally
                {
                    serverProcess.TrySignalAsync(CommandSignal.ControlC).Wait();
                    serverProcess.StandardInput.Close();
                    if (!serverProcess.Process.WaitForExit(5000))
                    {
                        serverProcess.Kill();
                        output.WriteLine("E2E process didn't exit!");
                    }
  
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}