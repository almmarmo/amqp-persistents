using AmqpLite = Amqp;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lib.Amqp
{
    public class PersistentConsumer : IPersistentConsumer, IDisposable
    {
        private const int MSLOOPING_DELAY = 5;
        private const int CREDIT_PUMP = 1;

        private AmqpLite.ConnectionFactory? connectionFactory;
        private AmqpLite.Connection? connection;
        private AmqpLite.Session? session;
        private AmqpLite.ReceiverLink? receiver;
        private readonly string schema;
        private readonly string user;
        private readonly string password;
        private readonly string host;
        private readonly int port;
        private readonly string address;
        private readonly int? msLoopingDelay;
        private readonly int? creditPump;
        private readonly ILogger logger;

        public PersistentConsumer(string schema, string user, string password, string host, int port, string address, int? msLoopingDelay, int? creditPump, ILogger logger)
        {
            this.schema = schema;
            this.user = user;
            this.password = password;
            this.host = host;
            this.port = port;
            this.address = address;
            this.msLoopingDelay = msLoopingDelay;
            this.creditPump = creditPump;
            this.logger = logger;
        }

        private async Task Setup()
        {
            var address = new AmqpLite.Address($"{schema}://{user}:{password}@{host}:{port}");

            connectionFactory = new AmqpLite.ConnectionFactory();
            connectionFactory.SSL.RemoteCertificateValidationCallback
                += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

            connection = await connectionFactory.CreateAsync(address);
            session = new AmqpLite.Session(connection);
            session.Closed += Session_Closed;

            logger?.LogInformation("Connection setup");
        }

        private void Session_Closed(AmqpLite.IAmqpObject sender, AmqpLite.Framing.Error error)
        {
            logger?.LogWarning($"session closed! d:{error?.Description}");
        }

        public async Task StartReceive(Func<IReceiverLinkWrapper, Task> processMessage)
        {
            if (session == null || session.IsClosed)
                await Setup();
            receiver ??= new AmqpLite.ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);


            while (true)
            {
                if (session != null && session.IsClosed)
                {
                    logger?.LogWarning("Reconnecting...");
                    await Task.Delay(100);
                    await Setup();
                    
                    receiver ??= new AmqpLite.ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);

                    logger?.LogWarning("Connections objects restarted.");
                }

                receiver.Start(creditPump ?? CREDIT_PUMP, async (link, message) =>
                {
                    try
                    {
                        await processMessage.Invoke(new ReceiverLinkWrapper(link, message));
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "Error on receiving message.");
                    }
                });

                await Task.Delay(msLoopingDelay ?? MSLOOPING_DELAY);
            }
        }

        public async Task ReceiveAsync(Func<IReceiverLinkWrapper, Task> processMessage)
        {
            if (session == null || session.IsClosed)
                await Setup();

            receiver ??= new AmqpLite.ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);

            receiver.Start(1, async (link, message) =>
            {
                try
                {
                    await processMessage.Invoke(new ReceiverLinkWrapper(link, message));
                    message.Dispose();
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error on receiving message.");
                }
            });
        }

        public void Dispose()
        {
            receiver?.Close();
            session?.Close();
            connection?.Close();

            receiver = null;
            session = null;
            connection = null;

            logger?.LogInformation("Persistent consumer disposed.");
        }
    }
}
