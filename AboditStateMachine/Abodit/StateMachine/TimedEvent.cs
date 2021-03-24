using System;

namespace Abodit.StateMachine
{
    /// <summary>
    /// A event that should fire at a particular time, used inside a StateMachine
    /// </summary>
    /// <remarks>
    /// Serializable so you can persist a state machine to disk if necessary
    /// TEvent must be IEquatable also
    /// </remarks>
    [Serializable]
    public class TimedEvent<TEvent> where TEvent : IEquatable<TEvent>
    {
        /// <summary>
        /// The time at which the event should happen
        /// </summary>
        public DateTimeOffset AtUtc { get; set; }

        /// <summary>
        /// The event that happens
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Serialization.
        public TEvent Event { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Serialization.

        /// <summary>
        /// Autorepeat after this many ticks
        /// </summary>
        /// <remarks>
        /// Would have used a TimeSpan but they don't XML Serialize
        /// </remarks>
        public long AutoRepeatTicks { get; set; }

        /// <summary>
        /// Get the next occurrence (after autorepeat)
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset NextOccurrence()
        {
            return this.AtUtc.AddTicks(this.AutoRepeatTicks);
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString() => $"{this.AtUtc} {this.Event} repeat = {this.AutoRepeatTicks}";
    }
}