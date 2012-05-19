using System;
using System.Collections.Generic;
using System.Messaging;

namespace MessageUtilities
{
    public static class MessageExtensions
    {
        public static void TraceMessage(this Message requestMessage)
        {
            Console.WriteLine("Sent request");
            Console.WriteLine("\tTime:       {0}", DateTime.Now.ToString("HH:mm:ss.ffffff"));
            Console.WriteLine("\tMessage ID: {0}", requestMessage.Id);
            Console.WriteLine("\tCorrel. ID: {0}", requestMessage.CorrelationId);
            Console.WriteLine("\tContents:   {0}", requestMessage.Body);
        }

        public static void Each<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }
    }
}
