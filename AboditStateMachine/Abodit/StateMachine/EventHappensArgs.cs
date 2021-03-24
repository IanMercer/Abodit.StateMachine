using System;

namespace Abodit.StateMachine
{
    /// <summary>
    /// Arguments for an event that is fired when an Event happens
    /// </summary>
    public struct EventHappensArgs<TContext, TEvent>
    {
        /// <summary>
        /// The DateTimeOffset of the Event
        /// </summary>
        public DateTimeOffset DateTimeOffset { get; set; }

        /// <summary>
        /// The context
        /// </summary>
        public TContext Context { get; set; }

        /// <summary>
        /// The event
        /// </summary>
        public TEvent Event { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="EventHappensArgs{TContext, TEvent}"/> class
        /// </summary>
        public EventHappensArgs(TContext context, TEvent @event, DateTimeOffset dateTimeOffset)
        {
            this.Context = context;
            this.Event = @event;
            this.DateTimeOffset = dateTimeOffset;
        }
    }
}