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
            //TODO: If the channel does not exist create it, otherwise attach to it
        }

        public void Send(string message)
        {
            var requestMessage = new Message {Body = message};

            //TODO: Send the message over the queue.

            requestMessage.TraceMessage();
        }

    }
}
