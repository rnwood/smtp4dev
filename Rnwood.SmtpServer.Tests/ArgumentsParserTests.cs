using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Tests
{
    
    public class ArgumentsParserTests
    {
        [Fact]
        public void Parsing_FirstArgumentAferVerbWithColon_Split()
        {
            ArgumentsParser args = new ArgumentsParser("ARG1=VALUE:BLAH");
            Assert.Equal(1, args.Arguments.Length);
            Assert.Equal("ARG1=VALUE:BLAH", args.Arguments[0]);
        }

        [Fact]
        public void Parsing_MailFrom_WithDisplayName()
        {
            ArgumentsParser args = new ArgumentsParser("<Robert Wood<rob@rnwood.co.uk>> ARG1 ARG2");
            Assert.Equal("<Robert Wood<rob@rnwood.co.uk>>", args.Arguments[0]);
            Assert.Equal("ARG1", args.Arguments[1]);
            Assert.Equal("ARG2", args.Arguments[2]);
        }

        [Fact]
        public void Parsing_MailFrom_EmailOnly()
        {
            ArgumentsParser args = new ArgumentsParser("<rob@rnwood.co.uk> ARG1 ARG2");
            Assert.Equal("<rob@rnwood.co.uk>", args.Arguments[0]);
            Assert.Equal("ARG1", args.Arguments[1]);
            Assert.Equal("ARG2", args.Arguments[2]);
        }
    }
}