using Amqp;
using Lib.Amqp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ReconkeyMonitoring
{
    public class PersistentConsumer : IPersistentConsumer, IDisposable
    {
        private ConnectionFactory connectionFactory;
        private Connection connection;
        private Session session;
        private ReceiverLink receiver;
        private readonly string schema;
        private readonly string user;
        private readonly string password;
        private readonly string host;
        private readonly int port;
        private readonly string address;
        private readonly ILogger logger;

        //private readonly NLog.ILogger logger;

        public PersistentConsumer(string schema, string user, string password, string host, int port, string address, ILogger logger)
        {
            this.schema = schema;
            this.user = user;
            this.password = password;
            this.host = host;
            this.port = port;
            this.address = address;
            this.logger = logger;

            //logger = NLog.LogManager.LogFactory.GetLogger("PersistentConsumer");
        }

        private async Task Setup()
        {
            var address = new Address($"{schema}://{user}:{password}@{host}:{port}");

            connectionFactory = new ConnectionFactory();
            connectionFactory.SSL.RemoteCertificateValidationCallback
                += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

            connection = await connectionFactory.CreateAsync(address);
            session = new Session(connection);
            session.Closed += Session_Closed;

            logger.LogInformation("Connection setup");
        }

        private void Session_Closed(IAmqpObject sender, Amqp.Framing.Error error)
        {
            logger.LogWarning($"session closed! d:{error?.Description}");
            //receiver.Detach(error);
            //persistentConsumer = false;
        }

        public async Task StartReceive(Func<IReceiverLink, Message, Task> processMessage)
        {
            if (session == null || session.IsClosed)
                await Setup();
            if (receiver == null)
                receiver = new ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);


            while (true)
            {
                if (session != null && session.IsClosed)
                {
                    logger.LogWarning("Reconnecting...");
                    await Setup();
                    if (receiver == null)
                        receiver = new ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);

                    logger.LogWarning("Connections objects restarted.");
                }

                receiver.Start(1, async (link, message) =>
                {
                    try
                    {
                        await processMessage.Invoke(link, message);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR!");
                        logger.LogError(e.Message);
                    }
                });

                await Task.Delay(10);
            }
        }

        public async Task ReceiveAsync(Func<IReceiverLink, Message, Task> processMessage)
        {
            if (session == null || session.IsClosed)
                await Setup();

            receiver ??= new ReceiverLink(session, $"amq-console-consumer={Guid.NewGuid()}", address);

            receiver.Start(1, async (link, message) =>
            {
                try
                {
                    await processMessage.Invoke(link, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR!");
                    logger.LogError(e.Message);
                }
                //await receiver.DetachAsync();
            });
        }

        public void Dispose()
        {
            receiver?.Close();
            session?.Close();
            connection?.Close();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            receiver = null;
            session = null;
            connection = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            logger.LogInformation("Persistent consumer disposed.");
        }
    }
}
