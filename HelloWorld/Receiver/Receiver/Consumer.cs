using System;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer
    {
    	private readonly string _channelName;
    	private readonly MessageQueue _channel;

        public Consumer(string channelName)
        {
        	_channelName = channelName;
        	_channel = new MessageQueue(_channelName) {Formatter = new XmlMessageFormatter(new[] {typeof (string)})};
        	_channel.MessageReadPropertyFilter.SetAll();
        }

    	public void Consume()
        {
			Message message = _channel.Receive();
			//Console.WriteLine(message.Body.ToString());
			if (message != null)
			{
				message.TraceMessage();
			}
        }
    }
}