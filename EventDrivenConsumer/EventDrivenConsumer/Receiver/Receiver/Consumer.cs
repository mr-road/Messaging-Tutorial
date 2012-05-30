using System;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer 
    {
        private readonly MessageQueue _channel;
        private bool _isRunning;

        public Consumer(string channelName)
        {
            //We need to identify how the message is formatted - xml is the default
			

            _channel = new MessageQueue(channelName) {Formatter = new XmlMessageFormatter(new[] {typeof (string)})};
            //We want to trace message headers such as correlation id, so we need to tell MSMQ to retrieve those
            _channel.MessageReadPropertyFilter.SetAll();
            //TODO: Set up a callback for the recieve completed event that calls the Consume method
        	_channel.ReceiveCompleted += Consume;
        }

        public void Start()
        {
            _isRunning = true;
        	Receive();
            Console.WriteLine("Service started");
        }


        public void Pause()
        {
            _isRunning = false;
            Console.WriteLine("Service paused");
        }

        public void Stop()
        {
            _isRunning = false;
            _channel.Close();
            Console.WriteLine("Service stopped");
        }

        private void Consume(object source, ReceiveCompletedEventArgs result)
        {
            //TODO: The Consume method is called on completion of a message being received

            //TODO: We need to obtain the message from the result by calling EndRecieve
            //TODO: We also want to begin recieving again, unless we have stopped running

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
			if (_isRunning)
			{
				_channel.BeginReceive(new TimeSpan(0, 0, 0, ConfigurationSettings.PollingTimeout));
			}
		}
    }
}