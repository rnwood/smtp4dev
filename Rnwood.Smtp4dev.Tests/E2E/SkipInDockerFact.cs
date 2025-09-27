using System;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public sealed class SkipInDockerFact : FactAttribute
    {
        public SkipInDockerFact()
        {
            if (IsDockerMode())
            {
                Skip = "This test is not compatible with Docker execution due to custom port requirements.";
            }
        }

        private static bool IsDockerMode()
            => Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY") == "docker";
    }
}