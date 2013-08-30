using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

/*
 *  使用到的DLL應該也要註冊，這樣RTD Server執行時，才能取到所需的DLL
 */

namespace FirstRTD
{    
    /// <summary>
    /// http://weblogs.asp.net/kennykerr/archive/2008/11/13/Rtd3.aspx
    /// </summary>
    [
        Guid("9AA100A8-E50E-4047-9C60-E4732391063E"),
        ProgId("KGI.TW.Der.FirstRTD.2"),
    ]
    public class MyRTSServer : IRtdServer
    {
        private IRTDUpdateEvent m_callback = null;
        private Timer m_timer;
        private int m_topicId;       
        private EventLog logOSEvent = new EventLog();

        public MyRTSServer()
        {
            try
            {
                logOSEvent.Source = "MyRTSServer";                
                logOSEvent.WriteEntry("[MyRTSServer] ctr()"); 
            }
            catch(Exception exp )
            {
                logOSEvent.WriteEntry("Error");
            }
        }

        #region IRtdServer 成員

        public int ServerStart(IRTDUpdateEvent callback)
        {
            if (logOSEvent != null)
                logOSEvent.WriteEntry("ServerStart() "); 

            m_callback = callback;
            m_timer = new Timer(TimerFunc, null, Timeout.Infinite, Timeout.Infinite);
            return 1;
        }

        public object ConnectData(int topicId, ref Array strings, ref bool newValues)
        {
            if (logOSEvent != null)
            {
                logOSEvent.WriteEntry(string.Format("ConnectData() {0}", topicId));
                foreach (string s in strings)
                {
                    logOSEvent.WriteEntry(s);
                }
            }

            m_topicId = topicId;
            m_timer.Change(1000, 1000);
            return GetTime();

        }

        public Array RefreshData(ref int topicCount)
        {
            if (logOSEvent != null)
            {
                logOSEvent.WriteEntry(string.Format("RefreshData()"));                
            }

            object[,] data = new object[2, 1];
            data[0, 0] = m_topicId;
            data[1, 0] = GetTime();

            topicCount = 1;

            m_timer.Change(1000, 1000);
            return data;
        }

        public void DisconnectData(int topicId)
        {
            if (logOSEvent != null)
            {
                logOSEvent.WriteEntry(string.Format("DisconnectData() {0}", topicId));
            }

            m_timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public int Heartbeat()
        {
            return 1;
        }

        public void ServerTerminate()
        {
            if (logOSEvent != null)
            {
                logOSEvent.WriteEntry(string.Format("ServerTerminate()"));
            }

            if (null != m_timer)
            {
                m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                m_timer = null;
            }
        }

        #endregion

        private void TimerFunc(object data)
        {
            m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_callback.UpdateNotify();
        }

        private string GetTime()
        {
            return DateTime.Now.ToString("hh:mm:ss:ff");
        }

    }

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

    [Guid("A43788C1-D91B-11D3-8F39-00C04F3651B8")]
    public interface IRTDUpdateEvent
    {
        void UpdateNotify();

        int HeartbeatInterval { get; set; }

        void Disconnect();
    }
}
