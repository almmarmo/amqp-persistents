namespace Lib.Amqp
{
    public interface IReceiverLinkWrapper
    {
        void AcceptMessage();
        object GetMessageBody();
        void RejectMessage();
    }
}