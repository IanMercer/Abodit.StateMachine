using System;

namespace Abodit.StateMachine
{
    /// <summary>
    /// Arguments for an event that is fired when the state transitions
    /// </summary>
    public struct StateTransitionArgs<TContext, TState>
    {
        /// <summary>
        /// The DateTimeOffset when the state transition happened
        /// </summary>
        public DateTimeOffset DateTimeOffset { get; set; }

        /// <summary>
        /// The context object
        /// </summary>
        public TContext Context { get; set; }

        /// <summary>
        /// The old state
        /// </summary>
        public TState OldState { get; set; }

        /// <summary>
        /// The new state
        /// </summary>
        public TState NewState { get; set; }

        /// <summary>
        /// Creates a new <see cref="StateTransitionArgs{TContext, TState}"/>
        /// </summary>
        public StateTransitionArgs(TContext context, TState oldState, TState newState, DateTimeOffset dateTimeOffset)
        {
            this.Context = context;
            this.OldState = oldState;
            this.NewState = newState;
            this.DateTimeOffset = dateTimeOffset;
        }
    }
}