using Google.Cloud.PubSub.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightConsole
{
    enum OnMode
    {
        On, Off, Dim
    }

    class GHEventArgs : EventArgs
    {
        public GHEventArgs(OnMode mode, int level = 0)
            : base()
        {
            Mode = mode;
            Level = level;
        }

        public OnMode Mode { get; private set; }

        /// <summary>
        /// Unused for Off mode
        /// </summary>
        public int Level { get; private set; }
    }

    class GHLightsClient : IDisposable
    {
        /// <summary>
        /// If client does not pull the message within this many seconds, we'll receive the message
        /// on the next pull
        /// </summary>
        private const int AckDeadline = 60;
        private static readonly List<string> _mDimsWords;
        private static readonly List<string> _mOnWords;
        private static readonly List<string> _mOffWords;

        static GHLightsClient()
        {
            _mDimsWords = new List<string>()
            {
                "dim", "half"
            };

            _mOnWords = new List<string>()
            {
                "on", "full"
            };

            _mOffWords = new List<string>()
            {
                "off", "dark", "darkness"
            };
        }

        #region Fields
        private SubscriberClient _mSubscriber;
        private SubscriptionName _mSubscriptionName;
        private readonly object _mSync = new object();
        private bool _mIsDisposed;

        /// <summary>
        /// Set on launch so we know to reject old message on app launch. Otherwise
        /// the lighting control may go nuts if a bunch of queued up messages arrive
        /// at one time.
        /// </summary>
        private readonly DateTime _mLiveAt;
        #endregion

        /// <summary>
        /// Constructs a new Google Home subscriber
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="topicId"></param>
        public GHLightsClient(string projectId, string subscriptionId, string topicId)
        {
            TopicName topicName = new TopicName(projectId, topicId);
            _mLiveAt = DateTime.Now;

            _mSubscriber = SubscriberClient.Create();
            _mSubscriptionName = new SubscriptionName(projectId, subscriptionId);

            try
            {
                _mSubscriber.CreateSubscription(_mSubscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: AckDeadline);
            }
            catch (Exception)
            { }

            Task.Factory.StartNew(() => AwaitMessages());
        }

        /// <summary>
        /// Raised when a new Google Home Lighting event arrives
        /// </summary>
        public event EventHandler<GHEventArgs> OnGHLightsEvent;

        /// <summary>
        /// Notifies subscriber of new event
        /// </summary>
        /// <param name="e">Event details</param>
        protected void RaiseRaiseGHLightsEvent(GHEventArgs e)
        {
            OnGHLightsEvent?.Invoke(this, e);
        }

        public void Dispose()
        {
            if(_mIsDisposed)
            {
                return;
            }

            lock (_mSync)
            {
                _mSubscriber.DeleteSubscriptionAsync(_mSubscriptionName);
                _mIsDisposed = true;
            }
        }

        /// <summary>
        /// Async subscriber task that await pub sub messages on the configured topic
        /// </summary>
        private async void AwaitMessages()
        {
            while(true)
            {
                lock(_mSync)
                {
                    if (_mIsDisposed)
                    {
                        return;
                    }
                }


                PullResponse response = await _mSubscriber.PullAsync(_mSubscriptionName, returnImmediately: true, maxMessages: 10);
                foreach (ReceivedMessage received in response.ReceivedMessages)
                {
                    PubsubMessage msg = received.Message;                    
                    Console.WriteLine($"Received message {msg.MessageId} published at {msg.PublishTime.ToDateTime()}");
                    Console.WriteLine($"Text: '{msg.Data.ToStringUtf8()}'");

                    // Only handle message sent while this subscriber has been active
                   // if(msg.PublishTime.ToDateTime() >= _mLiveAt)
                    {
                        var str = msg.Data.ToStringUtf8().Trim('"').Split();
                        
                        if(_mOffWords.Intersect(str).Count() > 0)
                        {
                            RaiseRaiseGHLightsEvent(new GHEventArgs(OnMode.Off));
                        }
                        else if(_mOnWords.Intersect(str).Count() > 0)
                        {
                            RaiseRaiseGHLightsEvent(new GHEventArgs(OnMode.On, level: 100));
                        }
                        else if (_mDimsWords.Intersect(str).Count() > 0)
                        {
                            RaiseRaiseGHLightsEvent(new GHEventArgs(OnMode.Dim, level: 35));
                        }
                    }
                }

                if (response.ReceivedMessages.Count > 0)
                {
                    // Acknowledge the message(s) even if internally we tossed them
                    _mSubscriber.Acknowledge(_mSubscriptionName, response.ReceivedMessages.Select(m => m.AckId));
                }
            }
        }
    }
}
