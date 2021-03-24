using System;

namespace Abodit.StateMachine
{
    /// <summary>
    /// The default time provider uses DateTime.UtcNow
    /// </summary>
    public class DefaultTimeProvider : TimeProvider
    {
        /// <summary>
        /// A singleton instance of the default time provider
        /// </summary>
        public static readonly TimeProvider Instance = new DefaultTimeProvider();

        /// <summary>
        /// Now
        /// </summary>
        public override DateTimeOffset Now
        {
            get { return DateTimeOffset.Now; }
            set { throw new Exception("Cannot set the time for the default time provider"); }
        }
    }
}