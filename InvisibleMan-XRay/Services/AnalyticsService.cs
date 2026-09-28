using System;

namespace InvisibleManXRay.Services
{
    using Analytics;

    public class AnalyticsService : Service
    {
        public void Setup(
            Func<string> getClientId,
            Func<bool> getSendingAnalyticsEnabled,
            Func<string> getApplicationVersion
        )
        {
        }

        public void SendEvent(IEvent analyticsEvent, bool isForced = false)
        {
        }
    }
}
