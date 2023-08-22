using Amqp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Amqp
{
    public interface IPersistentConsumer
    {
        Task StartReceive(Func<IReceiverLinkWrapper, Task> processMessage);
    }
}
