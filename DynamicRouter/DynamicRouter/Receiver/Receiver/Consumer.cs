using System;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer 
    {
        private readonly MessageQueue channel;
        private bool isRunning;

        public Consumer(string channelName, string topic, string controlChannelName)
        {
            //TODO: Attach to a control channel exposed by the broker, name in config file

            channel = new MessageQueue(channelName) {Formatter = new XmlMessageFormatter(new[] {typeof (string)})};
            channel.MessageReadPropertyFilter.SetAll();
            channel.ReceiveCompleted += Consume;
        }

        public void Start()
        {
            isRunning = true;
            Subscribe();
            Receive();
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
            try
            {
                var queue = (MessageQueue) source;
                var message = queue.EndReceive(result.AsyncResult);
                if (message != null)
                {
                    message.TraceMessage();
                }
            }
            catch (MessageQueueException mqe)
            {
                Console.WriteLine("{0} {1}", mqe.Message, mqe.MessageQueueErrorCode);
            }

            Receive();
        }

        private void Receive()
        {
            if (isRunning)
            {
                channel.BeginReceive(new TimeSpan(0, 0, 0, ConfigurationSettings.PollingTimeout));
            }
        }

        private void Subscribe()
        {
            //TODO: Send a message on the control channel in the format Topic:Channel to subscribe this service to messages on that topic in the Broker
        }
    }
}