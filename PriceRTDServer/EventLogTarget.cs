using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PriceRTDServer
{
    public class EventLogTarget
    {
        private static EventLogTarget instance = new EventLogTarget();
        public static EventLogTarget Instance
        {
            get
            {
                return instance;
            }
        }

        private EventLogTarget()
        {
            log.Source = "PriceRTDServer";
        }

        private EventLog log = new EventLog();

        public void WriteLog( string msg )
        {
            log.WriteEntry( msg ); 
        }
    }
}
