using System;
using System.Collections.Generic;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    [Flags]
    internal enum Queues
    {
        None = 0,
        Input = 1,
        Control = 2
    }

    internal class MessageBroker 
    {
        private readonly MessageQueue inputChannel;
        private bool isRunning;
        private readonly IDictionary<string, MessageQueue> routingTable = new Dictionary<string, MessageQueue>();  

        public MessageBroker(string inputChannelName)
        {
            inputChannel = EnsureQueueExists(inputChannelName);

            inputChannel.MessageReadPropertyFilter.SetAll();

            inputChannel.ReceiveCompleted += Route;

            //TODO: Create a control channel to recieve routing information from subscribers 
            //HINT: Create a control channel in the Configuration Settings, pass into this method
            //TODO: Add a recieve completed event to add subscribers to call Subscribe (see below)
        }


        public void Start()
        {
            isRunning = true;
            Receive(Queues.Input | Queues.Control);
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
            inputChannel.Close();
            Console.WriteLine("Service stopped");
        }

        public MessageQueue EnsureQueueExists(string channelName)
        {
            var channel = !MessageQueue.Exists(channelName) ? MessageQueue.Create(channelName) : new MessageQueue(channelName);
            channel.Formatter = new XmlMessageFormatter(new[] {typeof (string)});
            return channel;
        }

        private void Route(object source, ReceiveCompletedEventArgs result)
        {
            try
            {
                var queue = (MessageQueue) source;
                var message = queue.EndReceive(result.AsyncResult);

                TraceMessage(message);

                var topic = Convert.ToBase64String(message.Extension);
                Console.WriteLine("Message Topic is {0", topic);

                var targetQueue = routingTable[topic];
                targetQueue.Send(message);

            }
            catch (MessageQueueException mqe)
            {
                Console.WriteLine("{0} {1}", mqe.Message, mqe.MessageQueueErrorCode);
            }

            Receive(Queues.Input);
        }

        private void Receive(Queues queuesToListenOn)
        {
            //TODO: When we recieve a message we need to subscibe to notifications from the queue again, this method lets us subscribe to one or both
            if (isRunning)
            {
                if (queuesToListenOn.HasFlag(Queues.Input))
                {
                    inputChannel.BeginReceive(new TimeSpan(0, 0, 0, ConfigurationSettings.PollingTimeout));
                }


                if (queuesToListenOn.HasFlag(Queues.Control))
                {
                    //TODO: subscribe to messages on the control queue
                }
            }
        }

        private void Subscribe(object source, ReceiveCompletedEventArgs result)
        {
            //TODO: Get the message off the contro queue
            //TODO: parse the message (format is Topic:QueueName)
            //TODO: Create queue if does not exist or attach to queue
            //TODO: Add mappint to the routing table for topic and queue
            //TODO: Resubsrcibe to the control queue
        }

        private static void TraceMessage(Message message)
        {
            if (message != null)
            {
                message.TraceMessage();
            }
        }
    }
}