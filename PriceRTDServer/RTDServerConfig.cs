using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace PriceRTDServer
{
    public class RTDServerConfig
    {
        private static string regRoot = @"SOFTWARE\RTDPriceServer";
        private RegistryKey keyRoot = null;

        private static RTDServerConfig instance = new RTDServerConfig();
        public static RTDServerConfig Instance
        {
            get
            {
                return instance;
            }
        }

        static RTDServerConfig()
        {
        }

        private RTDServerConfig()
        {
            keyRoot = Registry.LocalMachine.OpenSubKey(regRoot, true);
            if (keyRoot == null)
                keyRoot = Registry.LocalMachine.CreateSubKey(regRoot, RegistryKeyPermissionCheck.ReadWriteSubTree); 
        }

        public string ServiceCode
        {
            get
            {
                return keyRoot.GetValue("ServiceCode", "17001").ToString();
            }
            set
            {
                keyRoot.SetValue("ServiceCode", value);
            }
        }

        public string Network
        {
            get
            {
                return keyRoot.GetValue("Network", "").ToString();
            }
            set
            {
                keyRoot.SetValue("Network", value);
            }
        }

        public string Daemon
        {
            get
            {
                return keyRoot.GetValue("Daemon", "10.32.242.41:7500").ToString();
            }
            set
            {
                keyRoot.SetValue("Daemon", value);
            }
        }
    }
}
