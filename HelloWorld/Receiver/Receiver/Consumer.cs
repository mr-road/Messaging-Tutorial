using System;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer
    {
        private readonly MessageQueue channel;

        public Consumer(string channelName)
        {
            //TODO: Attach to a message queue identified in channelName. 
            //TODO: Set the formatter
            //TODO: We want to trace message headers such as correlation id, so we need to tell MSMQ to retrieve those by setting the property filter
        }

        public void Consume()
        {
            //TODO: recieve a message on the queue
            //TODO: Trace the message out to the command line. HINT: Use the extension method.
        }
    }
}