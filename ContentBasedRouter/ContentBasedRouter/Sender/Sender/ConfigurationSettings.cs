using System.Configuration;

namespace Sender
{
    class ConfigurationSettings
    {
        public ConfigurationSettings()
        {
            OutBoundChannel = ConfigurationManager.AppSettings["OutputChannel"];
        }

        public static string OutBoundChannel { get; set; }
    }
}
