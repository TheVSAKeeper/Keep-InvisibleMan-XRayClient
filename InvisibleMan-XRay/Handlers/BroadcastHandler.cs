namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class BroadcastHandler : Handler
    {
        public Status CheckForBroadcast()
        {
            return new Status(Code.ERROR, SubCode.BROADCAST_UNAVAILABLE, null);
        }
    }
}
