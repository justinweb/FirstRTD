using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TIBCO.Rendezvous;
using System.Runtime.InteropServices;

namespace KGI.TW.Der.ST.MsgChannel
{
    /// <summary>
    /// 使用Tibco RV模式傳送訊息的管道
    /// </summary>
    public class TibcoMsgChannel : IMsgChannel
    {
        /// <summary>
        /// 計算使用Tibco/RV的物件數，以便在沒有使用的物件時，可以清除Tibco/RV資源
        /// </summary>
        protected static int tibcoConuter = 0;
        /// <summary>
        /// Tibco Transport物件
        /// </summary>
        protected Transport tbTransport = null;
        /// <summary>
        /// Tibco Listener
        /// </summary>
        protected Listener tbListener = null;
        /// <summary>
        /// Tibco Queue
        /// </summary>
        protected Queue tbQueue = null;
        /// <summary>
        /// Tibco Dispatcher
        /// </summary>
        protected Dispatcher tbDispatcher = null;
        /// <summary>
        /// 用來執行Tibco Dispatcher的執行緒
        /// </summary>
        protected Thread tQueue = null;
        /// <summary>
        /// 記錄訂閱的主題及它的Listener
        /// </summary>
        protected Dictionary<string, Listener> dicListener = new Dictionary<string, Listener>();
        /// <summary>
        /// Tibco Service
        /// </summary>
        public string Service = "";
        /// <summary>
        /// Tibco Network
        /// </summary>
        public string Network = "";
        /// <summary>
        /// Tibco Daemon
        /// </summary>
        public string Daemon = "";
        /// <summary>
        /// 在Tibco RV web中可以顯示的transport名稱 
        /// </summary>
        public string Description = ""; 
        /// <summary>
        /// 產生Tibco訊息的時間格式
        /// </summary>
        public static readonly string BTStampFormat = "yyyyMMddhhmmssff";
        /// <summary>
        /// 建構子
        /// </summary>
        public TibcoMsgChannel()
        {
            if (Interlocked.Increment(ref tibcoConuter) == 1)
                TIBCO.Rendezvous.Environment.Open();
        }
        /// <summary>
        /// 建構子，產生物件時，直接指定要使用的<see cref="Service"/>、<see cref="Network"/>、<see cref="Daemon"/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="network"></param>
        /// <param name="daemon"></param>
        public TibcoMsgChannel( string service, string network, string daemon)
        {
            if (Interlocked.Increment(ref tibcoConuter) == 1)
                TIBCO.Rendezvous.Environment.Open();

            Service = service;
            Network = network;
            Daemon = daemon;   
        }

        // 2013/07/18 Justin 不再使用解構子，因為如果同時使用兩個TibcoMsgChannel時，一前一後使用下，後面的在傳送時，前面正好被GC而呼叫到解構子時，會誤將Tibco Library給Close()
        ///// <summary>
        ///// 解構子
        ///// </summary>
        //~TibcoMsgChannel()
        //{
        //    Destory();
        //}

        #region IMsgChannel 成員

        public event Action<bool> OnStatus;

        public event Action<string, Dictionary<string, string>> OnMessage;
                
        /// <inheridoc/>
        public void Connect()
        {            
            if (tbQueue == null) // 只有第一次才去建立物件 
            {
                tbQueue = new Queue();
                try
                {
                    NetTransport nt = new NetTransport(Service, Network, Daemon);
                    nt.Description = Description;
                    tbTransport = nt;
                }
                catch (Exception exp)
                {
                    if (tbQueue != null)
                    {
                        tbQueue.Destroy();
                        tbQueue = null;
                    }
                    throw;
                }
            }

            // 啟動執行緒去派送Tibco Queue
            tQueue = new Thread(QueueDispatch);
            tQueue.IsBackground = true;
            tQueue.Start();            
        }

        /// <inheridoc/>
        public void DisConnect()
        {
            if (tQueue != null)
            {
                tQueue.Abort();              
            }

            if (tbQueue != null)
            {
                tbQueue.Destroy();
                tbQueue = null;
            }
            if (tbTransport != null)
                tbTransport.Destroy();
            if (tbDispatcher != null)
                tbDispatcher.Destroy();
        }

        /// <inheridoc/>
        public void Destory()
        {
            if (Interlocked.Decrement(ref tibcoConuter) == 0)
                TIBCO.Rendezvous.Environment.Close();
        }

        /// <inheridoc/>
        public void Subscribe(string subject)
        {
            if (tbQueue == null || tbTransport == null)
            {
                throw new InvalidOperationException("IMsgChannel is not connected"); 
            }

            lock (dicListener)
            {
                if (dicListener.ContainsKey(subject))
                    return;

                Listener tbListener = new Listener(tbQueue, tbTransport, subject, null);
                tbListener.MessageReceived += new MessageReceivedEventHandler(tbListener_MessageReceived);

                dicListener.Add(subject, tbListener);
            }
        }

        /// <inheridoc/>
        public void UnSubscribe(string subject)
        {
            lock (dicListener)
            {
                if (dicListener.ContainsKey(subject))
                {
                    Listener tbListener = dicListener[subject];
                    tbListener.MessageReceived -= new MessageReceivedEventHandler(tbListener_MessageReceived);
                    dicListener.Remove(subject);                    

                    tbListener.Destroy();
                }
            }
        }

        /// <inheridoc/>
        public void Send(string subject, Dictionary<string, string> msgData)
        {
            if (tbTransport != null)
            {
                TIBCO.Rendezvous.Message msg = new Message();
                msg.SendSubject = subject; 
                Dic2Msg(ref msg, msgData);

                tbTransport.Send(msg);
            }
        }

        #endregion
        /// <summary>
        /// 傳送Tibco訊息
        /// </summary>
        /// <param name="subject">傳送主題</param>
        /// <param name="msg">Tibco的訊息物件</param>
        public void Send(string subject, TIBCO.Rendezvous.Message msg)
        {
            if (tbTransport != null)
            {
                msg.SendSubject = subject;
                tbTransport.Send(msg);
            }
        }

        /// <summary>
        /// 派送Tibco Queue的執行緒
        /// </summary>
        /// <param name="status"></param>
        private void QueueDispatch(object status)
        {
            tbDispatcher = new Dispatcher(tbQueue);
            tbDispatcher.Join(); 
        }

        protected void tbListener_MessageReceived(object listener, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            if (OnMessage != null && listener is Listener)
            {                
                Dictionary<string, string> tmpData = Msg2Dic(messageReceivedEventArgs.Message); 
                OnMessage( ((Listener)listener).Subject, tmpData );
            }
        }

        #region 共用函式
        /// <summary>
        /// 將Tibco Message轉成Dictionary格式
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Msg2Dic(TIBCO.Rendezvous.Message msg)
        {
            Dictionary<string, string> htField = new Dictionary<string, string>();
            TIBCO.Rendezvous.MessageField entry;
            int numfields = Convert.ToInt32(msg.FieldCount);
            if (numfields > 0)
            {
                for (uint i = 0; i < numfields; i++)
                {
                    entry = msg.GetFieldByIndex(i);
                    if (entry != null)
                    {
                        string fieldname = entry.Name;
                        string fielddata = entry.Value.ToString();                        
                        htField[fieldname] = fielddata;
                    }
                }
            }
            return htField;
        }
        /// <summary>
        /// 將Dictionary轉成Tibco的Message物件
        /// </summary>
        /// <param name="msg">轉成的Tibco訊息</param>
        /// <param name="data">要轉換的資料</param>
        public static void Dic2Msg( ref TIBCO.Rendezvous.Message msg , Dictionary<string,string> data )
        {           
            foreach (KeyValuePair<string, string> pair in data)
            {
                // 2013/05/17 Justin
                // 因為Message.GetField()函式會花很多時間，只好改用try/catch，
                // 所以外部使用此函式時，最好能確認Message物件時不包含Dictionary中已有的欄位
                try
                {
                    msg.AddField(pair.Key, pair.Value);
                }
                catch (Exception exp)
                {
                     
                }
            }
        }
        #endregion

    }    

    
}
