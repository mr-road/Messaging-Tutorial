using System.Messaging;
using MessageUtilities;

namespace Sender
{
    class Producer
    {
        private readonly MessageQueue channel;

        public Producer(string channelName)
        {
            //assume that the broker has created any required channels for comms with it, otherwise just error
            channel = new MessageQueue(channelName) {Formatter = new XmlMessageFormatter(new []{typeof(string)})};
        }

        public void Send(Message message)
        {
            channel.Send(message);

            message.TraceMessage();
        }

    }
}
