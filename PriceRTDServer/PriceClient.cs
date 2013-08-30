using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KGI.TW.Der.ST.MsgChannel;

namespace PriceRTDServer
{
    public class PriceClient
    {
        public event Action<string, decimal> OnUpdatePrice = null;

        private IMsgChannel msgChannel = null;

        public PriceClient(IMsgChannel msgChannel)
        {
            this.msgChannel = msgChannel;
            this.msgChannel.OnMessage += new Action<string, Dictionary<string, string>>(msgChannel_OnMessage);
        }

        void msgChannel_OnMessage(string subject, Dictionary<string, string> dicData)
        {
            try
            {
                string symbol = "";
                string sDeal = "";
                if (dicData.TryGetValue("Symbol", out symbol) && dicData.TryGetValue("Deal", out sDeal))
                {
                    decimal deal = 0.0m;
                    decimal.TryParse(sDeal, out deal);

                    if (OnUpdatePrice != null)
                    {
                        OnUpdatePrice(symbol, deal);
                        //EventLogTarget.Instance.WriteLog(string.Format("PriceClient notify {0} @ {1}", symbol, deal)); 
                    }
                }
            }
            catch (Exception exp)
            {
                EventLogTarget.Instance.WriteLog("PriceClient::OnMessage() failed");
            }
        }

        public void Subscribe(string subject)
        {
            if (msgChannel != null)
            {
                msgChannel.Subscribe(subject);
            }
            else
            {
                EventLogTarget.Instance.WriteLog("PriceClient::Subscribe() msgChannel is null"); 
            }
        }

        public void UnSubscribe(string subject)
        {
            if (msgChannel != null)
            {
                msgChannel.UnSubscribe(subject);
            }
            else
            {
                EventLogTarget.Instance.WriteLog("PriceClient::UnSubscribe() msgChannel is null");
            }
        }

    }
}
