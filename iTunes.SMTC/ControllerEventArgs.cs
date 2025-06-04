using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTunes.SMTC
{
    public class ControllerInitializedEventArgs : EventArgs
    {
        public string Key { get; internal set; }
    }

    public class ControllerDestroyedEventArgs : EventArgs
    {
        public string Key { get; internal set; }
    }

    public class AMControllerInitializedEventArgs : ControllerInitializedEventArgs
    {
        public Uri ServiceUri { get; internal set; }
    }
}
