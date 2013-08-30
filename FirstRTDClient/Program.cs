using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstRTD;

namespace FirstRTDClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IRtdServer rtdServer = new MyRTSServer();
            rtdServer.ServerStart(new MyFirstRTDCallback());             
        }
    }

    public class MyFirstRTDCallback : IRTDUpdateEvent
    {
        #region IRTDUpdateEvent 成員

        public void UpdateNotify()
        {            
        }

        public int HeartbeatInterval
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Disconnect()
        {            
        }

        #endregion
    }
}
