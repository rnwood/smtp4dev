using Medallion.Shell;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
namespace Rnwood.Smtp4dev.Desktop.Tests
{
    public class LaunchTests
    {
        private readonly ITestOutputHelper output;

        public LaunchTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void LaunchAndCheckUILoaded()
        {
            RunE2ETest(ctx => { });

        }


        protected void RunE2ETest(Action<E2ETestContext> test)
        {

            string workingDir = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_WORKINGDIR");
            string binary = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");
            List<string> args = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_ARGS")?.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                ?.ToList() ?? new List<string>();

            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetFullPath("../../../../Rnwood.Smtp4dev.Desktop");
            }
            else
            {
                workingDir = Path.GetFullPath(workingDir);
            }

            if (string.IsNullOrEmpty(binary))
            {
                binary = "dotnet";

                //.NETCoreapp,Version=v3.1
                string framework = typeof(Rnwood.Smtp4dev.Desktop.Program)
                    .Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>()?
                    .FrameworkName;

                //netcoreapp3.1
                string folder = framework.TrimStart('.').Replace("CoreApp,Version=v", "").ToLower();

                string mainModule = Path.GetFullPath($"../../../../Rnwood.Smtp4dev.Desktop/bin/Debug/{folder}/Rnwood.Smtp4dev.Desktop.dll");
                args.Insert(0, mainModule);

            }

            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
            string dbPath = Path.GetTempFileName();
            File.Delete(dbPath);

            args.AddRange(new[] {
                "--debugsettings", $"--db={dbPath}", "--nousersettings"
            }.Where(a => a != ""));


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
                            baseUrl = new Uri($"http://localhost:{portNumber}");
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

        public class E2ETestContext
        {
            public Uri BaseUrl { get; set; }
            public int SmtpPortNumber { get; set; }

            public int ImapPortNumber { get; set; }
        }
    }



}

