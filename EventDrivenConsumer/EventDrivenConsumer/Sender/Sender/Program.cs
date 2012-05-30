using System;
using NDesk.Options;

namespace Sender
{
    class Program
    {
		private static string _channel;
		private static string _message;

        static void Main(string[] args)
        {
            _channel = "bob3";
            _message = "This is also a message";
			//var p = new OptionSet()
			//            {
			//                {"c|channel=", v => channel = v},
			//                {"m|message=", m => message = m}
			//            };
			//p.Parse(args);
            
			//if (CheckArguments(message, channel))
			//{
			//    return;
			//}

            string channelName = string.Format(@".\private$\{0}", _channel);

            var producer = new Producer(channelName);
        	for (int i = 0; i < 5; i++)
        	{
				producer.Send(_message);
        	}
        	Console.ReadKey();
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
