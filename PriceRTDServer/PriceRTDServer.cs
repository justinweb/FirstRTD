using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using KGI.TW.Der.ST.MsgChannel;

namespace PriceRTDServer
{ 

    /// <summary>
    /// http://weblogs.asp.net/kennykerr/archive/2008/11/13/Rtd3.aspx
    /// </summary>
    [
        Guid("9AA100A8-E50E-4047-9C60-E4732391063E"),
        ProgId("KGI.TW.Der.PriceRTDServer.2"),
    ]
    public class PriceRTDServer : IRtdServer
    {
        private IRTDUpdateEvent m_callback = null;
        //private Timer m_timer;
        private int m_topicId;
        private EventLog logOSEvent = new EventLog();
        private string logTitle = "PriceRTDServer";
        private TibcoMsgChannel msgChannel = null;
        private PriceClient priceClient = null;        

        /// <summary>
        /// 記錄Client端的訂閱
        /// </summary>
        private Dictionary<string, List<int>> dicClient = new Dictionary<string, List<int>>(); // key = symbol, value = topicid
        private ReaderWriterLock rwlClient = new ReaderWriterLock();

        /// <summary>
        /// 記錄要更新給Client端的資訊
        /// </summary>
        private Dictionary<int, decimal> dicLatestDeal = new Dictionary<int, decimal>();
        private ReaderWriterLock rwlLatestDeal = new ReaderWriterLock();

        public PriceRTDServer()
        {
            try
            {
                logOSEvent.Source = logTitle;
                logOSEvent.WriteEntry("[PriceRTDServer] ctr()");
            }
            catch (Exception exp)
            {
                logOSEvent.WriteEntry("Error");
            }
        }

        #region IRtdServer 成員

        public int ServerStart(IRTDUpdateEvent callback)
        {
            WriteLog("ServerStart() ");

            try
            {
                msgChannel = new TibcoMsgChannel();
                msgChannel.Service = RTDServerConfig.Instance.ServiceCode;
                msgChannel.Network = RTDServerConfig.Instance.Network;
                msgChannel.Daemon = RTDServerConfig.Instance.Daemon;
                msgChannel.Description = "PriceRTDServer";
                msgChannel.Connect();

                priceClient = new PriceClient(msgChannel);
                priceClient.OnUpdatePrice += new Action<string, decimal>(priceClient_OnUpdatePrice);
                priceClient.Subscribe("ST.OMS.SERVER.Price.>");

                WriteLog(string.Format("MsgChannel {0},{1},{2}", RTDServerConfig.Instance.Daemon, RTDServerConfig.Instance.Network, RTDServerConfig.Instance.ServiceCode));
            }
            catch (Exception exp)
            {
                WriteLog("MsgChannel connect faile");
            }

            m_callback = callback;           
            return 1;
        }

        void priceClient_OnUpdatePrice(string symbol, decimal deal)
        {
            //EventLogTarget.Instance.WriteLog(string.Format("PriceRTDServer receive {0} @ {1}", symbol, deal));            

            // Get topicID
            List<int> topicID = QueryTopicID(symbol);

            #region update data and notify Excel
            if (topicID.Count > 0)
            {
                try
                {
                    rwlLatestDeal.AcquireWriterLock(Timeout.Infinite);
                    try
                    {
                        foreach (int topic in topicID)
                        {
                            dicLatestDeal[topic] = deal; 
                        }
                    }
                    finally
                    {
                        rwlLatestDeal.ReleaseWriterLock();
                    }
                }
                catch (Exception exp)
                {
                    //LogSystem.Instance.Error( exp.ToString() );         
                }

                if (m_callback != null)
                    m_callback.UpdateNotify();
            }
            #endregion
        }

        public object ConnectData(int topicId, ref Array strings, ref bool newValues)
        {
            WriteLog(string.Format("ConnectData() {0}", topicId));                

            m_topicId = topicId;                

            #region registe subscription
            if (strings.Length > 0)
            {
                string symbol = strings.GetValue(0).ToString();

                try
                {
                    rwlClient.AcquireWriterLock(Timeout.Infinite);
                    try
                    {
                        if (dicClient.ContainsKey(symbol))
                        {
                            dicClient[symbol].Add(topicId);
                        }
                        else
                        {
                            dicClient.Add(symbol, new List<int>() { topicId });
                        }
                        WriteLog(string.Format("ConnectData() {0}=>{1}", topicId, symbol ));                
                    }
                    finally
                    {
                        rwlClient.ReleaseWriterLock();
                    }
                }
                catch (Exception exp)
                {
                    //LogSystem.Instance.Error( exp.ToString() );         
                }
            }
            #endregion

            return "0.0";

        }

        public Array RefreshData(ref int topicCount)
        {
            //WriteLog(string.Format("RefreshData()"));

            try
            {
                rwlLatestDeal.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    topicCount = dicLatestDeal.Count;
                    object[,] data = new object[2, topicCount];
                    int i = 0;
                    foreach (KeyValuePair<int, decimal> pair in dicLatestDeal)
                    {
                        data[0, i] = pair.Key;
                        data[1, i] = pair.Value;
                        i++;
                    }
                    return data;
                }
                finally
                {
                    rwlLatestDeal.ReleaseWriterLock();
                }
            }
            catch (Exception exp)
            {
                //LogSystem.Instance.Error( exp.ToString() );         
                topicCount = 0;
                return new object[0, 0];
            }          
        }

        public void DisconnectData(int topicId)
        {
            WriteLog(string.Format("DisconnectData() {0}", topicId));
            
            try
            {
                rwlClient.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    foreach (string key in dicClient.Keys)
                    {
                        dicClient[key].Remove(topicId);
                    }
                }
                finally
                {
                    rwlClient.ReleaseWriterLock();
                }
            }
            catch (Exception exp)
            {
                //LogSystem.Instance.Error( exp.ToString() );         
            }
        }

        public int Heartbeat()
        {
            return 1;
        }

        public void ServerTerminate()
        { 
            WriteLog(string.Format("ServerTerminate()"));

            try
            {
                priceClient.UnSubscribe("ST.OMS.SERVER.Price.>");
                priceClient = null;

                msgChannel.DisConnect();
                msgChannel.Destory();

                #region 
                RTDServerConfig.Instance.ServiceCode = msgChannel.Service;
                RTDServerConfig.Instance.Network = msgChannel.Network; 
                RTDServerConfig.Instance.Daemon = msgChannel.Daemon;
                #endregion
            }
            catch (Exception exp)
            {
                WriteLog("MsgChannel disconnect failed");
            }
        }

        #endregion

        //private void TimerFunc(object data)
        //{
        //    m_timer.Change(Timeout.Infinite, Timeout.Infinite);
        //    m_callback.UpdateNotify();
        //}

        //private string GetTime()
        //{
        //    return DateTime.Now.ToString("hh:mm:ss:ff");
        //}

        private void WriteLog(string msg)
        {
            if (logOSEvent != null)
            {
                string logMsg = string.Format("[{0}] {1}", logTitle, msg);
                logOSEvent.WriteEntry(logMsg);
            }
        }

        private List<int> QueryTopicID(string symbol)
        {
            List<int> result = new List<int>();
            try
            {
                rwlClient.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    if (dicClient.ContainsKey(symbol))
                    {
                        result.AddRange(dicClient[symbol]);                         
                    }
                }
                finally
                {
                    rwlClient.ReleaseReaderLock();
                }
            }
            catch (Exception exp)
            {
                //LogSystem.Instance.Error( exp.ToString() );         
            }

            return result;
        }

    }

   
}
