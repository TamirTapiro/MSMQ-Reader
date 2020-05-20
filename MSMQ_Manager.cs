using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MSMQ_Reader
{
    public class MSMQ_Manager
    {
        public MessageQueue[] PrivateQueues { get; set; }

        public MSMQ_Manager()
        {
            var machine = ConfigurationManager.AppSettings["MachineName"];
            PrivateQueues = MessageQueue.GetPrivateQueuesByMachine(machine);
        }

        public MessageQueue GetQueueByName(string queueName)
        {
            return PrivateQueues.Single(q => q.QueueName == queueName);
        }
        public string ReadMessage(Message msg)
        {
            StreamReader sr = new StreamReader(msg.BodyStream);
            string messageBody = "";
            while (sr.Peek() >= 0)
            {
                messageBody += sr.ReadLine();
            }
            return messageBody;
        }


    }
}
