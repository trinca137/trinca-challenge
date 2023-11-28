namespace Domain.Events
{
    public class BbqConfirmed : IEvent
    {
        public BbqConfirmed(bool confirmed)
        {
            Confirmed = confirmed;
        }

        public bool Confirmed { get; }
    }
}
