using Amqp;

namespace Lib.Amqp
{
    public class ReceiverLinkWrapper : IReceiverLinkWrapper
    {
        private readonly IReceiverLink receiverLink;
        private readonly Message message;

        public ReceiverLinkWrapper(IReceiverLink receiverLink, Message message)
        {
            this.receiverLink = receiverLink;
            this.message = message;
        }

        public void AcceptMessage()
        {
            receiverLink.Accept(message);
        }

        public void RejectMessage()
        {
            receiverLink.Reject(message);
        }

        public object GetMessageBody()
        {
            return message.Body;
        }
    }
}
