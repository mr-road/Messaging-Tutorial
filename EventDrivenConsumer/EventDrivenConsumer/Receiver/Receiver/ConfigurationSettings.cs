﻿using System;
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
        }

        public static int PollingInterval { get; set; }
        public static int PollingTimeout { get; set; }
        public static string ChannelName { get; set; }
    }
}
