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

            //TODO: Create an instance of the producer to send to the broker via the outbound channel
            //TODO: Create a message from the input and set its Extension property from the topic 
            //HINT: Use Convert from and to Base 64 string to convert a string to bytes and vice-versa
            //TODO: Send the message
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
