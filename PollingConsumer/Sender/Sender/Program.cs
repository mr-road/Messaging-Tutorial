using System;
using NDesk.Options;

namespace Sender
{
    class Program
    {
        //sender -m="my name is" -c=polling_consumer
        static void Main(string[] args)
        {
            var channel = string.Empty;
            var message = string.Empty;
            var p = new OptionSet()
                        {
                            {"c|channel=", v => channel = v},
                            {"m|message=", m => message = m}
                        };
            p.Parse(args);
            
            if (CheckArguments(message, channel))
            {
                return;
            }

            string channelName = string.Format(@".\private$\{0}", channel);

            var producer = new Producer(channelName);
            producer.Send(message);
        }
        
        private static bool CheckArguments(string message, string channel)
        {
            bool errors = false;
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("You must provide a channel");
                errors = true;
            }

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("You must provide a message");
                errors = true;
            }
            return errors;
        }
    }
}
