﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public interface IParameterProcessor
    {
        void SetParameter(string key, string value);
    }
}
