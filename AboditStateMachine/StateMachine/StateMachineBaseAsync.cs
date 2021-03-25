using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Abodit.StateMachine
{
    /// <summary>
    /// The base StateMachine
    /// </summary>
    public abstract partial class StateMachineBaseAsync<TState, TEvent, TContext> :
        IStateMachineAsync<TState, TEvent, TContext>
        where TEvent : IEquatable<TEvent>
    {
        /// <summary>
        /// The current state of this State machine
        /// </summary>
        public TState CurrentState { get; set; }

        /// <summary>
        /// The time (compare UTC) at which this state machine needs to receive a wake up from an external timing component
        /// </summary>
        public DateTimeOffset NextTimedEventAt { get; set; }

        /// <summary>
        /// A public Event that advises of any changes in state that actually happen
        /// </summary>
        public event Action<StateTransitionArgs<TContext, TState>>? StateChanges;

        /// <summary>
        /// Creates a new <see cref="StateMachineBaseAsync{TState, TEvent, TContext}"/>
        /// </summary>
        protected StateMachineBaseAsync(TState initialState)
        {
            this.CurrentState = initialState;
        }

        /// <summary>
        /// Called prior to the state actually changing
        /// </summary>
        protected virtual void OnStateChanging(TContext context, TState oldState, TState newState)
        {
        }

        /// <summary>
        /// Called after the state has changed
        /// </summary>
        protected virtual void OnStateChanged(TContext context, TState oldState, TState newState)
        {
            this.StateChanges?.Invoke(new StateTransitionArgs<TContext, TState>(context, oldState, newState, TimeProvider.Current.Now));
        }

        /// <summary>
        /// A public Event that advises of any events that have happened
        /// </summary>
        /// <remarks>
        /// Can thus expose timer events to the outside world without changing state
        /// </remarks>
        public event Action<EventHappensArgs<TContext, TEvent>>? EventHappened;

        /// <summary>
        /// Fires when this event happens
        /// </summary>
        protected void OnEventHappens(TContext context, TEvent @event)
        {
            this.EventHappened?.Invoke(new EventHappensArgs<TContext, TEvent>(context, @event, TimeProvider.Current.Now));
        }

        /// <summary>
        /// Future events set on this StateMachine (Serializable, so public and settable, but use it only for serialization)
        /// </summary>
        /// <remarks>
        /// Each StateMachine has its own small set of future events.  Typically this list will be very small; when an event fires
        /// it might record a future event that needs to be executed
        /// </remarks>
        public List<TimedEvent<TEvent>> TimedEvents { get; set; } = new List<TimedEvent<TEvent>>();

        /// <summary>
        /// Set the timed events
        /// </summary>
        /// <param name="value"></param>
        public void SetTimedEvents(IEnumerable<TimedEvent<TEvent>> value)
        {
            this.TimedEvents = value.ToList();
            this.RecalculateNextTimedEvent();
        }

        /// <summary>
        /// Recalculate the next timed event
        /// </summary>
        protected void RecalculateNextTimedEvent()
        {
            if (this.TimedEvents.Any())
                this.NextTimedEventAt = this.TimedEvents.Min(te => te.AtUtc);
            else
                this.NextTimedEventAt = DateTimeOffset.MaxValue;      // never!
        }

        /// <summary>
        /// Most State machines have a Start() method that moves them to their initial state
        /// </summary>
        /// <remarks>
        /// Not really needed, use constructor with State value, deprecate?
        /// </remarks>
        public virtual void Start(TState state)
        {
            this.CurrentState = state;
        }

        /// <summary>
        /// Find any events that should have fired by now and execute up to a maximum set number of them
        /// (e.g. use the number to limit how long this method can run for in worst case before you persist the state
        ///  machine to disk)
        /// </summary>
        /// <param name="now">The current time (passed as a parameter to make this method testable)</param>
        /// <param name="context">A context object</param>
        /// <param name="limitOnNumberExecuted">In order to prevent runaway execution of a misconfigured recurring event set a maximum number of executions</param>
        /// <remarks>
        /// Note: These events are executed synchronously on the calling thread
        /// Caller should persist this object (if necessary) after all timedEvents have been processed
        /// Timed events may themselves add new events to the event queue.  These new events will happen
        /// immediately in this method if they themselves are already past due
        /// </remarks>
        public virtual async Task Tick(DateTimeOffset now, TContext context, int limitOnNumberExecuted = 10000)
        {
            // incompatible with async ... lock (noConcurrentTicks)
            {
                while (limitOnNumberExecuted-- > 0)
                {
                    TEvent nextEvent = default(TEvent);
                    DateTimeOffset eventAtUtc = default(DateTimeOffset);
                    lock (this.TimedEvents)
                    {
                        TimedEvent<TEvent>? current = null;
                        // Within the lock all we do is find the next event, remove it and update the next time
                        current = this.TimedEvents.Where(te => te.AtUtc <= now).OrderBy(te => te.AtUtc).FirstOrDefault();
                        if (current != null)
                        {
                            nextEvent = current.Event;
                            eventAtUtc = current.AtUtc;

                            if (current.AutoRepeatTicks != 0)
                            {
                                current.AtUtc = current.NextOccurrence();
                            }
                            else
                            {
                                this.TimedEvents.Remove(current);       // Completed, don't reschedule
                            }
                            RecalculateNextTimedEvent();
                        }
                    }

                    // NOTE: Current.At is now pointing to the new time

                    if (nextEvent is TEvent)
                    {
                        // DEBUG
                        if (eventAtUtc > now) Trace.WriteLine(string.Format("BUG: {0}, is due at {1:HHmm} and it is now {2:HHmm}", nextEvent, eventAtUtc, now));
                        //else Trace.WriteLine(string.Format("OK: {0} was due at {1:HHmm} and it is now {2:HHmm}", nextEvent, eventAtUtc, utcNow));
                        await this.EventHappens(nextEvent, context);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// At a certain time, cause a certain event to happen
        /// </summary>
        public void At(DateTimeOffset dateTime, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                var utc = dateTime.UtcDateTime;
                this.TimedEvents.Add(new TimedEvent<TEvent> { AtUtc = utc, Event = @event, AutoRepeatTicks = 0 });
                if (utc < this.NextTimedEventAt) this.NextTimedEventAt = utc;
            }
        }

        /// <summary>
        /// At a certain time Utc, cause a certain event to happen
        /// </summary>
        public void At(DateTime dateTimeUtc, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                this.TimedEvents.Add(new TimedEvent<TEvent> { AtUtc = dateTimeUtc, Event = @event, AutoRepeatTicks = 0 });
                if (dateTimeUtc < this.NextTimedEventAt) this.NextTimedEventAt = dateTimeUtc.ToUniversalTime();
            }
        }

        /// <summary>
        /// After a certain period, cause a certain event to happen
        /// </summary>
        public void After(TimeSpan timeSpan, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                var dateTimeAt = TimeProvider.Current.Now.Add(timeSpan);
                At(dateTimeAt, @event);
            }
        }

        /// <summary>
        /// After a certain period, cause a certain event to happen, but only if that event
        /// is already present in the queue
        /// </summary>
        public void Bump(TimeSpan timeSpan, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                if (this.TimedEvents.RemoveAll(e => e.Event.Equals(@event)) > 0)
                {
                    var dateTimeAt = TimeProvider.Current.Now.Add(timeSpan);
                    this.At(dateTimeAt, @event);
                }
            }
        }

        /// <summary>
        /// After a certain period, cause a certain event to happen, but if that event is already
        /// in the queue, bump it to the new time
        /// </summary>
        public void AfterOrBump(TimeSpan timeSpan, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                this.TimedEvents.RemoveAll(e => e.Event.Equals(@event));
                var dateTimeAt = TimeProvider.Current.Now.Add(timeSpan);
                this.At(dateTimeAt, @event);
            }
        }

        /// <summary>
        /// Every time interval, cause a certain event to happen
        /// </summary>
        /// <remarks>
        /// Uses TimePeriod not timespan because TimePeriod is more flexible (e.g. weeks, months, ...)
        /// Use CancelScheduledEvent() to remove a repeated event
        /// </remarks>
        public void Every(TimeSpan timeSpan, TEvent @event)
        {
            lock (this.TimedEvents)
            {
                var firstOccurrence = TimeProvider.Current.Now.Add(timeSpan);
                this.TimedEvents.Add(new TimedEvent<TEvent> { AtUtc = firstOccurrence, Event = @event, AutoRepeatTicks = timeSpan.Ticks });
                if (firstOccurrence < this.NextTimedEventAt) this.NextTimedEventAt = firstOccurrence;
            }
        }

        /// <summary>
        /// Removes any scheduled or recurring events that would fire the given event
        /// </summary>
        public void CancelScheduledEvent(TEvent @event)
        {
            lock (this.TimedEvents)
            {
                this.TimedEvents.RemoveAll(te => te.Event.Equals(@event));
                RecalculateNextTimedEvent();
            }
        }

        /// <summary>
        /// An event has happened, transition to next state
        /// </summary>
        public abstract Task EventHappens(TEvent @event, TContext context);
    }
}