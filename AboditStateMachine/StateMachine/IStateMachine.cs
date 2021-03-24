using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abodit.StateMachine
{
    /// <summary>
    /// Interface for a StateMachine
    /// </summary>
    public interface IStateMachineAsync<TState, TEvent, TContext> where TEvent : IEquatable<TEvent>
    {
        /// <summary>
        /// Get the current state. Calling set will not trigger entry and exit actions, use only for serialization.
        /// </summary>
        TState CurrentState { get; set; }

        // DateTime NextTimedEventAt { get; set; }
        // not part of interface, used only for serialization ...List<StateMachine<TEvent, TContext>.TimedEvent> TimedEvents { get; set; }

        /// <summary>
        /// An event has happened
        /// </summary>
        event Action<EventHappensArgs<TContext, TEvent>> EventHappened;

        /// <summary>
        /// The State has changed
        /// </summary>
        event Action<StateTransitionArgs<TContext, TState>> StateChanges;

        /// <summary>
        /// Sets the TimedEvents on the state machine (used only during loading from persistence)
        /// </summary>
        void SetTimedEvents(IEnumerable<TimedEvent<TEvent>> value);

        /// <summary>
        /// Start the state machine in the given state
        /// </summary>
        void Start(TState initialState);

        /// <summary>
        /// The Tick method must be called regularly if you want timed events to work
        /// </summary>
        /// <param name="now"></param>
        /// <param name="context"></param>
        /// <param name="limitOnNumberExecuted"></param>
        Task Tick(DateTimeOffset now, TContext context, int limitOnNumberExecuted = 10000);

        /// <summary>
        /// An external event happens, trigger the state machine
        /// </summary>
        /// <param name="event"></param>
        /// <param name="context"></param>
        Task EventHappens(TEvent @event, TContext context);
    }
}