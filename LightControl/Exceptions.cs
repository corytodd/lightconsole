using System;

namespace LightControl
{
    /// <summary>
    /// This class of exception explicitly states the issue and is not something
    /// that can be resovled at runtime in the program. The error itself it non-
    /// terminal but it does require physical action on the user's behalf
    /// </summary>
    public class Exceptions : Exception
    {
        public static readonly string Error = "Gateway is not Sync Mode. Press sync button on gateway";
        public Exceptions() : base(Error)
        { }
    }

    /// <summary>
    /// Internal exception that states the communication should be attempted again
    /// </summary>
    class MalformedGWR : Exception
    {
        public static readonly string Error = "Gateway response was malformed";
        public MalformedGWR() : base(Error)
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
