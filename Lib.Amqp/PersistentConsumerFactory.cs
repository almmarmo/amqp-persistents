using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lib.Amqp
{
    public class PersistentConsumerFactory
    {
        private readonly PersistentConsumerOptions options;
        private readonly ILoggerFactory loggerFactory;

        public PersistentConsumerFactory(IOptions<PersistentConsumerOptions> options, ILoggerFactory loggerFactory)
        {
            this.options = options.Value;
            this.loggerFactory = loggerFactory;
        }

        public PersistentConsumer Create() => new PersistentConsumer(options.Schema, options.User, options.Password, options.Host, options.Port, options.Address, options.MsLoopingDelay, options.CreditPump, loggerFactory?.CreateLogger<PersistentConsumer>()!);
    }
}
