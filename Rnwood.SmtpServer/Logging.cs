using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer
{
    public class Logging
    {
        private static ILoggerFactory _factory = new LoggerFactory();

        public static ILoggerFactory Factory
        {
            get
            {
                return _factory;
            }
        }
    }
}