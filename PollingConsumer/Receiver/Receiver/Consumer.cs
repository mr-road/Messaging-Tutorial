using System;
using System.Timers;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer : IDisposable
    {
		private readonly string _channelName;
		private readonly MessageQueue _channel;
        private readonly Timer _timer;

        public Consumer(string channelName)
        {
        	_channelName = channelName;

        	//TODO: Attach to a message queue
            //TODO: Set the formatter so we can read the messages
            //TODO: We want to trace message headers such as correlation id, so we need to tell MSMQ to retrieve those
			_channel = new MessageQueue(_channelName) { Formatter = new XmlMessageFormatter(new[] { typeof(string) }) };
			_channel.MessageReadPropertyFilter.SetAll();

            //we use a timer to poll the queue at a regular interval, of course this may need to be re-entrant but we have no state to worry about
            _timer = new Timer(ConfigurationSettings.PollingInterval) {AutoReset = true};

			_timer.Elapsed += Consume;
            //TODO: on the Timer's Elapsed event we want to consume messages, so set the callback to our Consume method
        }

		private void Consume(object state, ElapsedEventArgs args)
		{
			try
			{
				var message = _channel.Receive(new TimeSpan(TimeSpan.TicksPerSecond * ConfigurationSettings.PollingTimeout));
				if (message != null)
				{
					message.TraceMessage();
				}
			}
			catch (MessageQueueException mqe)
			{
				Console.WriteLine("{0} {1}", mqe.Message, mqe.MessageQueueErrorCode);
			}
		}

    	public void Start()
        {
            _timer.Start();
            Console.WriteLine("Service started, will read queue every {0} ms", ConfigurationSettings.PollingInterval);
        }

        public void Pause()
        {
            _timer.Stop();
            Console.WriteLine("Service paused");
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Close();
            //TODO: Shut the queue
            Console.WriteLine("Service stopped");
        }

        //TODO: A callback method for the Elasped event that recieves a message from the queue
        //TODO: Set a timeout on the recieve call using the polling timeout configuration setting

        public void Dispose()
        {
           _timer.Close(); 
        }
    }
}