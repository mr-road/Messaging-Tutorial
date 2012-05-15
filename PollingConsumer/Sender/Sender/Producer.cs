using System.Messaging;
using MessageUtilities;

namespace Sender
{
    class Producer
    {
        private MessageQueue channel;

        public Producer(string channelName)
        {
            EnsureQueueExists(channelName);
        }

        public void EnsureQueueExists(string channelName)
        {
            channel = !MessageQueue.Exists(channelName) ? MessageQueue.Create(channelName) : new MessageQueue(channelName);
        }

        public void Send(string message)
        {
            var requestMessage = new Message {Body = message};

            channel.Send(requestMessage);

            requestMessage.TraceMessage();
        }

    }
}
