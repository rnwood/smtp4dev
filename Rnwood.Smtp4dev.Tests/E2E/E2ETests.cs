using Medallion.Shell;
using System;
using System.Collections.Generic;
using System.IO;
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
            string mainModule = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");

            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetFullPath("../../../../Rnwood.Smtp4dev");
            }

            if (string.IsNullOrEmpty(mainModule))
            {
                //.NETCoreapp,Version=v3.1
                string framework = typeof(Program)
                    .Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>()?
                    .FrameworkName;

                //netcoreapp3.1
                string folder = framework.TrimStart('.').Replace("CoreApp,Version=v", "").ToLower();

                mainModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev/bin/Debug/{folder}/Rnwood.Smtp4dev.dll");
            }

            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
            string dbPath = Path.GetTempFileName();
            File.Delete(dbPath);


            List<string> args = new List<string>
            {
                mainModule, "--debugsettings", options.InMemoryDB ? "--db=" : $"--db={dbPath}", "--nousersettings", "--urls=http://*:0",
                "--imapport=0", "--smtpport=0", "--tlsmode=StartTls"
            };
            if (!string.IsNullOrEmpty(options.BasePath))
            {
                args.Add($"--basepath={options.BasePath}");
            }

            using (Command serverProcess = Command.Run("dotnet", args,
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
                    serverProcess.Kill();
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}