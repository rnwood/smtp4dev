// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class PlainMechanismProcessorTests
    {

        [Fact]
        public void ValidateCredentials_Correct()
        {
            var creds = new PlainAuthenticationCredentials("a", "b");
            bool result = creds.ValidateResponse("b");

            Assert.True(result);
        }

        [Fact]
        public void ValidateCredentials_Incorrect()
        {
            var creds = new PlainAuthenticationCredentials("a", "b");
            bool result = creds.ValidateResponse("c");

            Assert.False(result);
        }
    }
}
