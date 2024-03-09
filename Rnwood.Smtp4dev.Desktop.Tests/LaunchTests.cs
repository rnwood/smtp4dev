using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Forms;
namespace Rnwood.Smtp4dev.Desktop.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void LaunchAndCheckUILoaded()
        {
            string workingDir = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_WORKINGDIR");
            string binary = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");


            using FlaUI.Core.Application app = FlaUI.Core.Application.Launch(new ProcessStartInfo(binary, "--smtpport=0") { WorkingDirectory = workingDir });
            using var automation = new UIA3Automation();

            app.WaitWhileMainHandleIsMissing();
            var window = app.GetMainWindow(automation);
            window.Move(0, 0);
     
          
            try
            {

                Retry.WhileException(() =>
                {
                    app.WaitWhileBusy();
                    window = app.GetMainWindow(automation);
                    var element = window.FindFirstDescendant(f => f.ByName("SMTP server", FlaUI.Core.Definitions.PropertyConditionFlags.MatchSubstring)) ??
                    throw new Exception( "Did not find status label");
                    var text = element.AsButton().Name;
                    if (!text.Contains("listening on port"))
                        throw new Exception("Server not listening: " + text);
                }, TimeSpan.FromSeconds(30), null, true);

            } finally
            {
                try
                {
                    string screenshotFileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".png");
                    window.CaptureToFile(screenshotFileName);
                    TestContext.AddTestAttachment(screenshotFileName);
                } catch (Exception e)
                {
                    TestContext.WriteLine($"Screenshot failed with exception: {e}");
                }

                app.Close();
            }


        }
    }
}
