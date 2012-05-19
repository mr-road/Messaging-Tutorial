using System;
using System.Configuration;

namespace Receiver
{
    public static class ConfigurationSettings
    {
        static ConfigurationSettings()
        {
            PollingInterval = Convert.ToInt32(ConfigurationManager.AppSettings["PollingInterval"]);
            PollingTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["PollingTimeout"]);
            InBoundChannelName = ConfigurationManager.AppSettings["InBoundChannelName"];
            ControlChannel = ConfigurationManager.AppSettings["ControlChannelName"];
        }

        public static string ControlChannel { get; set; }
        public static int PollingInterval { get; set; }
        public static int PollingTimeout { get; set; }
        public static string InBoundChannelName { get; set; }
    }
}
