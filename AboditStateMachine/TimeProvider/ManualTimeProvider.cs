using System;

namespace Abodit.StateMachine
{
    /// <summary>
    /// The ManualTimeProvider is good for testing - you can manually advance time as you wish to simulate real time
    /// </summary>
    public class ManualTimeProvider : TimeProvider
    {
        /// <summary>
        /// Get the Now time
        /// </summary>
        public override DateTimeOffset Now { get; set; }

        /// <summary>
        /// Create a new ManualTimeProvider for testing (starting at curent UtcNow time)
        /// </summary>
        public ManualTimeProvider() : this(DateTimeOffset.UtcNow) { }

        /// <summary>
        /// Create a new ManualTimeProvider for testing starting at a specific time
        /// </summary>
        public ManualTimeProvider(DateTimeOffset initialTime)
        {
            this.Now = initialTime;
            //Trace.WriteLine("Time started at " + this.now + " from " + initialTime);
        }

        /// <summary>
        /// Create a new ManualTimeProvider for testing starting at a specific year month day at 12:30:36
        /// </summary>
        public ManualTimeProvider(int year, int month, int day)
            : this(new DateTimeOffset(year, month, day, 12, 30, 36, TimeSpan.FromHours(-8)))
        {
        }

        /// <summary>
        /// Add a given interval to the current time
        /// </summary>
        public void Add(int days = 0, int hours = 0, int minutes = 0, int seconds = 0)
        {
            this.Now = this.Now.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
            //Trace.WriteLine("Time advanced to " + this.now);
        }
    }
}