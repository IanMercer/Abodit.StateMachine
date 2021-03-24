using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Abodit.StateMachine
{
    /// <summary>
    /// A state machine allows you to track state and to take actions when states change
    /// </summary>
    /// <remarks>
    /// This state machine provides a fluent interface for defining states and transitions
    ///
    /// When you inherit from this abstract base class you define the types you want to use
    /// for Events and States.
    ///
    /// NB This is a 'curiously self-referencing generic'. This ensures that State objects are not
    /// shared across two StateMachines even if they have the same generic arguments. It also
    /// makes some syntax nicer and eliminates casting on the consumption side.
    /// Other than that, I hate the pattern as it's confusingly complex.
    ///
    /// NB No other properties of the object being tracked should be stored in the State object,
    /// the State object should know only about the State name that it represents. (SRP)
    ///
    /// Also has timed events set to fire at some point in the future but provides no Timer internally because
    /// that will be implementation dependent.  For example the State may exist in a serialized form
    /// in a database with the NextTimedEventAt property exposed for indexing and some external timer fecthing
    /// records and calling the Tick() method.
    /// </remarks>
    [DebuggerDisplay("Current State = {CurrentState.Name}")]
    public abstract partial class StateMachineAsync<TStateMachine, TEvent, TContext> :
        StateMachineBaseAsync<StateMachineAsync<TStateMachine, TEvent, TContext>.State, TEvent, TContext>
        where TStateMachine : StateMachineAsync<TStateMachine, TEvent, TContext>
        where TEvent : IEquatable<TEvent>
    {
        /// <summary>
        /// Empty constructor for serialization only
        /// </summary>
        protected StateMachineAsync()
            : base(null!)
        {
            this.TimedEvents = new List<TimedEvent<TEvent>>();      // Will get deserialized events written in to it
            this.NextTimedEventAt = DateTimeOffset.MaxValue;
        }

        /// <summary>
        /// Construct a state machine with an initial state
        /// </summary>
        protected StateMachineAsync(State initial) : this()
        {
            this.CurrentState = initial;
        }

        /// <summary>
        /// An event has happened, transition to next state
        /// </summary>
        public override async Task EventHappens(TEvent @event, TContext context)
        {
            if (this.CurrentState is null)
            {
                var initialStateIfNotSet = definitions.First().Value;
                throw new NullReferenceException("You forgot to set the initial State, maybe you wanted to use " + initialStateIfNotSet);
            }
            var oldState = this.CurrentState;
            var newState = await this.CurrentState.OnEvent((TStateMachine)this, @event, context);
            if (!(newState is null) && (newState != this.CurrentState))
            {
                this.OnStateChanging(context, oldState, newState);
                this.CurrentState = newState;
                this.OnStateChanged(context, oldState, newState);
            }

            this.OnEventHappens(context, @event);
        }

        private static readonly IDictionary<string, StateDefinition> definitions = new Dictionary<string, StateDefinition>();

        // We remember the parents for States separately from the State object itself
        // to make it easier to deal with deserialized states (which lack the parent structure)

        /// <summary>
        /// Add a new state definition
        /// </summary>
        public static State AddState(string name,
            Func<TStateMachine, TEvent, State, TContext, Task>? entryAction = null,
            Func<TStateMachine, State, TEvent, TContext, Task>? exitAction = null,
            State? parent = null)
        {
            var stateDefinition = new StateDefinition(name, entryAction, exitAction, parent);
            definitions.Add(name, stateDefinition);
            return stateDefinition.GetState();
        }
    }
}