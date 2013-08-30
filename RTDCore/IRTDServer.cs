using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace KGI.TW.Der.RTD.Core
{
    [Guid("EC0E6191-DB51-11D3-8F3E-00C04F3651B8")]
    public interface IRtdServer
    {
        int ServerStart(IRTDUpdateEvent callback);

        object ConnectData(int topicId,
                           [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array strings,
                           ref bool newValues);

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]
        Array RefreshData(ref int topicCount);

        void DisconnectData(int topicId);

        int Heartbeat();

        void ServerTerminate();
    }
}
