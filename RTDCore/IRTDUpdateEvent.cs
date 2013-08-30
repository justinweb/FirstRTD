using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace KGI.TW.Der.RTD.Core
{
    [Guid("A43788C1-D91B-11D3-8F39-00C04F3651B8")]
    public interface IRTDUpdateEvent
    {
        void UpdateNotify();

        int HeartbeatInterval { get; set; }

        void Disconnect();
    }
}
