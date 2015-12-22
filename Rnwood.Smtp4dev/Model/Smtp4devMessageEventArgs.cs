using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class Smtp4devMessageEventArgs : EventArgs
    {
        public Smtp4devMessageEventArgs(ISmtp4devMessage message)
        {
            Message = message;
        }

        public ISmtp4devMessage Message { get; private set; }
    }
}