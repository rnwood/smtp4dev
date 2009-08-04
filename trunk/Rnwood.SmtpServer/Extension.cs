using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public abstract class Extension
    {
        public abstract ExtensionProcessor CreateExtensionProcessor(ConnectionProcessor processor);
    }

    public abstract class ExtensionProcessor
    {
        public abstract string[] GetEHLOKeywords();
    }
}
