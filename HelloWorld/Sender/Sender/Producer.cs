using System.Messaging;
using MessageUtilities;

namespace Sender
{
    class Producer
    {
    	private readonly string _channelName;
    	private MessageQueue _channel;

        public Producer(string channelName)
        {
        	_channelName = channelName;
			EnsureQueueExists();
        }

    	public void EnsureQueueExists()
    	{
			if (!MessageQueue.Exists(_channelName))
			{
				MessageQueue.Create(_channelName);
			}
			_channel = new MessageQueue(_channelName);
    	}

        public void Send(string message)
        {
            var requestMessage = new Message {Body = message};

            _channel.Send(requestMessage);

            requestMessage.TraceMessage();
        }

    }
}
