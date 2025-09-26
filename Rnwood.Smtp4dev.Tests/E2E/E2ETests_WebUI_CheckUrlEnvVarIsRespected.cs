using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_WebUI_CheckUrlEnvVarIsRespected : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_CheckUrlEnvVarIsRespected(ITestOutputHelper output) : base(output)
        {
        }

        [SkipInDockerFact]
        public void CheckUrlEnvVarIsRespected()
        {
            UITestOptions options = new UITestOptions();
            options.EnvironmentVariables["SERVEROPTIONS__URLS"] = "http://127.0.0.2:2345;";

            RunUITestAsync($"{nameof(CheckUrlEnvVarIsRespected)}", (page, baseUrl, smtpPortNumber) =>
            {
                Assert.Equal(2345, baseUrl.Port);
                return Task.CompletedTask;
            }, options);
        }
    }
}