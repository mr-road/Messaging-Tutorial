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
            ChannelName = ConfigurationManager.AppSettings["ChannelName"];
            Topic = ConfigurationManager.AppSettings["Topic"];
            ControlChannelName = ConfigurationManager.AppSettings["ControlChannelName"];
        }

        public static string ControlChannelName { get; set; }
        public static string Topic { get; set; }
        public static int PollingInterval { get; set; }
        public static int PollingTimeout { get; set; }
        public static string ChannelName { get; set; }
    }
}
