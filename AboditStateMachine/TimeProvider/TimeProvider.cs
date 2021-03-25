using System;
using System.Threading;

namespace Abodit.StateMachine
{
    /// <summary>
    /// TimeProvider allows for replacement of DateTimeOffset.UtcNow for testing and other situations
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultTimeProvider"/> calls through to DateTimeOffset.UtcNow.
    /// A <see cref="ManualTimeProvider"/> makes testing easy.
    /// A time provider must NEVER go backwards in time.
    /// </remarks>
    public abstract class TimeProvider
    {
        [ThreadStatic]
        private static TimeProvider? current = null;     // avoid constructor cycle, use null and >> below

        /// <summary>
        /// Set the current time provider
        /// </summary>
        public static TimeProvider Current
        {
            get { return current ?? DefaultTimeProvider.Instance; }
            set
            {
                //Trace.WriteLine("New time provider : " + value.GetType().Name);
                lastTimeStamp = 0;      // reset so that UtcNowUniqueTicks isn't forced past the new start time
                current = value ?? throw new ArgumentNullException("value");
            }
        }

        /// <summary>
        /// Earliest feasible date (1970,1,1)
        /// </summary>
        public static readonly DateTimeOffset EarliestFeasible = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0));

        private static long lastTimeStamp = 0;

        /// <summary>
        /// Get the current Utc tick value but increment it to make it unique for each call to this function
        /// so it can serve as a time stamp
        /// </summary>
        public long UtcNowUniqueTicks
        {
            get
            {
                int debugCheck = 0;
                long orig, newval;
                do
                {
                    if (debugCheck++ > 1000000) throw new Exception("Too many iterations - bug!");
                    orig = lastTimeStamp;
                    newval = this.Now.UtcTicks;
                    if (orig + 1 > newval) newval = orig + 1;
                } while (Interlocked.CompareExchange(ref lastTimeStamp, newval, orig) != orig);
                return newval;
            }
        }

        /// <summary>
        /// DateTimeOffset now
        /// </summary>
        public abstract DateTimeOffset Now { get; set; }

        /// <summary>
        /// Reverts to a time provider using DateTimeOffset.Now
        /// </summary>
        public static void ResetToDefault()
        {
            current = DefaultTimeProvider.Instance;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Now.ToString();
        }

        /// <summary>
        /// Start using a new time provider in a using block
        /// </summary>
        /// <param name="newTimeProvider">The new time provider to use</param>
        /// <returns>An IDisposable that will revert to the old time provider when disposed</returns>
        public static IDisposable StartUsing(TimeProvider newTimeProvider)
        {
            return new TimeProviderDisposable(newTimeProvider);
        }

        private class TimeProviderDisposable : IDisposable
        {
            private readonly TimeProvider pushed;

            public TimeProviderDisposable(TimeProvider newTimeProvider)
            {
                this.pushed = TimeProvider.Current;
                TimeProvider.Current = newTimeProvider;
            }

            public void Dispose()
            {
                TimeProvider.Current = pushed;
            }
        }
    }
}