using System;
using System.Timers;
using System.Messaging;
using MessageUtilities;

namespace Receiver
{
    internal class Consumer : IDisposable
    {
        private readonly MessageQueue channel;
        private readonly Timer timer;

        public Consumer(string channelName)
        {
            //TODO: Attach to a message queue
            //TODO: Set the formatter so we can read the messages
            //TODO: We want to trace message headers such as correlation id, so we need to tell MSMQ to retrieve those

            //we use a timer to poll the queue at a regular interval, of course this may need to be re-entrant but we have no state to worry about
            timer = new Timer(ConfigurationSettings.PollingInterval) {AutoReset = true};

            //TODO: on the Timer's Elapsed event we want to consume messages, so set the callback to our Consume method
        }

        public void Start()
        {
            timer.Start();
            Console.WriteLine("Service started, will read queue every {0} ms", ConfigurationSettings.PollingInterval);
        }

        public void Pause()
        {
            timer.Stop();
            Console.WriteLine("Service paused");
        }

        public void Stop()
        {
            timer.Stop();
            timer.Close();
            //TODO: Shut the queue
            Console.WriteLine("Service stopped");
        }

        //TODO: A callback method for the Elasped event that recieves a message from the queue
        //TODO: Set a timeout on the recieve call using the polling timeout configuration setting

        public void Dispose()
        {
           timer.Close(); 
        }
    }
}