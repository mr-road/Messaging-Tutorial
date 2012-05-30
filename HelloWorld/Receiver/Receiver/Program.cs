using System;

namespace Receiver
{
    class Program
    {
        //receiver -c=hello_world
        static void Main(string[] args)
        {
            string channel = "bob";
			//var p = new OptionSet() { { "c|channel=", "The name of the channel that we should send messages to", c => channel = c } };
			//p.Parse(args);
            if (string.IsNullOrEmpty(channel))
            {
                Console.WriteLine("You must provide a channel name");
            	Console.ReadKey();
                return;
            }

            string channelName = string.Format(@".\private$\{0}", channel);

            var consumer = new Consumer(channelName);
            consumer.Consume();
			Console.ReadKey();
        }
    }
}
