using System;
using NDesk.Options;

namespace Sender
{
    class Program
    {
    	private static string _channel;
    	private static string _message;
        //sender -m="my name is" -c=polling_consumer
        static void Main(string[] args)
        {
			_channel = "bob2";
			_message = "this is a message";

			//channel = ParsArgs(args, out message);
			//if (ValidateArgs(channel, message)) return;

			string channelName = string.Format(@".\private$\{0}", _channel);

            var producer = new Producer(channelName);
        	for (int i = 0; i < 5; i++)
        	{
				producer.Send(_message);
        	}
			
        	Console.ReadKey();
        }

    	private static bool ValidateArgs(string channel, string message)
    	{
    		if (CheckArguments(message, channel))
    		{
    			return true;
    		}
    		return false;
    	}

    	private static void ParsArgs(string[] args)
    	{
    		var p = new OptionSet
    		        	{
    		        		{"c|channel=", v => _channel = v},
    		        		{"m|message=", m => _message = m}
    		        	};
    		p.Parse(args);
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
