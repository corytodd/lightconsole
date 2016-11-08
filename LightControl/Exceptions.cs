using System;

namespace LightControl
{
    /// <summary>
    /// This class of exception explicitly states the issue and is not something
    /// that can be resovled at runtime in the program. The error itself it non-
    /// terminal but it does require physical action on the user's behalf
    /// </summary>
    public class NotInSyncModeException : Exception
    {
        public static readonly string Error = "Gateway is not Sync Mode. Press sync button on gateway";
        public NotInSyncModeException() : base(Error)
        { }
    }

    /// <summary>
    /// Internal exception that states the communication should be attempted again
    /// </summary>
    class MalformedGwr : Exception
    {
        public static readonly string Error = "Gateway response was malformed";
        public MalformedGwr() : base(Error)
        { }
    }

    /// <summary>
    /// Exception that states the target gateway is non-responsive.
    /// </summary>
    public class TcpGatewayUnavailable : Exception
    {
        public static readonly string Error = "Gateway could not be contacted";
        public TcpGatewayUnavailable() : base(Error)
        { }
    }


    /// <summary>
    /// Internal exception that states the GWR auth token was rejected. Generate a new one.
    /// </summary>
    class InvalidToken : Exception
    {
        public static readonly string Error = "Invalid Token - Renew the token";
        public InvalidToken() : base(Error)
        { }
    }
}
