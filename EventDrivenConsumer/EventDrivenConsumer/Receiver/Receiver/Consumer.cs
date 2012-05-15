using System;
using System.Messaging;

namespace Receiver
{
    internal class Consumer 
    {
        private readonly MessageQueue channel;
        private bool isRunning;

        public Consumer(string channelName)
        {
            //We need to identify how the message is formatted - xml is the default
            channel = new MessageQueue(channelName) {Formatter = new XmlMessageFormatter(new[] {typeof (string)})};
            //We want to trace message headers such as correlation id, so we need to tell MSMQ to retrieve those
            channel.MessageReadPropertyFilter.SetAll();
            //TODO: Set up a callback for the recieve completed event that calls the Consume method
        }

        public void Start()
        {
            isRunning = true;
            //TODO: On start we need to wait on messages from the queue
            Console.WriteLine("Service started");
        }


        public void Pause()
        {
            isRunning = false;
            Console.WriteLine("Service paused");
        }

        public void Stop()
        {
            isRunning = false;
            channel.Close();
            Console.WriteLine("Service stopped");
        }

        private void Consume(object source, ReceiveCompletedEventArgs result)
        {
            //TODO: The Consume method is called on completion of a message being received
            //TODO: We need to obtain the message from the result by calling EndRecieve
            //TODO: We also want to begin recieving again, unless we have stopped running
        }
    }
}