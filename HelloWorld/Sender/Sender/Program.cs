using System;
using NDesk.Options;

namespace Sender
{
    class Program
    {
        //sender -c=hello_world
        static void Main(string[] args)
        {
            string channel = string.Empty;
            var p = new OptionSet() {{"c|channel=", "The name of the channel that we should send messages to", c => channel = c}};
            p.Parse(args);
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("You must provide a channel name");
                return;
            }

            string channelName = string.Format(@".\private$\{0}", channel);

            var producer = new Producer(channelName);
            producer.Send("Hello World");
        }
    }
}
