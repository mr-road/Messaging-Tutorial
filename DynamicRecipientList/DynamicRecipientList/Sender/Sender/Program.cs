using System;
using System.Messaging;
using NDesk.Options;

namespace Sender
{
    class Program
    {
        //sender -m="my name is" -t=Greeting
        static void Main(string[] args)
        {
            var topic = string.Empty;
            var message = string.Empty;
            var p = new OptionSet()
                        {
                            {"t|topic=", t => topic = t},
                            {"m|message=", m => message = m}
                        };
            p.Parse(args);

            if (CheckArguments(message, topic))
            {
                return;
            }

            var producer = new Producer(ConfigurationSettings.OutBoundChannel);
            var msg = new Message(message) {Extension = Convert.FromBase64String(topic)};
            producer.Send(msg);

        }

        private static bool CheckArguments(string message, string topic)
        {
            bool errors = false;
            if (string.IsNullOrEmpty(topic))
            {
                Console.WriteLine("You must provide a topic");
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
