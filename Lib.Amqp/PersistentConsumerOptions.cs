namespace Lib.Amqp
{
    public class PersistentConsumerOptions
    {
        public string Schema { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Address { get; set; }
        public int? MsLoopingDelay { get; set; }
        public int? CreditPump { get; set; }
    }
}
