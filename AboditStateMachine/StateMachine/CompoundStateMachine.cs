using System;
using System.Threading.Tasks;

namespace Abodit.StateMachine
{
    /// <summary>
    /// A compound state machine is essentially a cross product of two state machines sharing the same event type.
    /// </summary>
    /// <remarks>
    /// See: http://en.wikipedia.org/wiki/UML_state_machine#Orthogonal_regions
    ///
    /// Wikipedia: In most real-life situations, however, orthogonal regions are only approximately orthogonal (i.e., they are not independent).
    /// Therefore, UML statecharts provide a number of ways for orthogonal regions to communicate and synchronize their behaviors.
    /// From these rich sets of (sometimes complex) mechanisms, perhaps the most important is that orthogonal regions can coordinate
    /// their behaviors by sending event instances to each other.
    /// </remarks>
    public class CompoundStateMachineAsync<TFirstStateMachine, TSecondStateMachine,
        TFirstStateMachineState, TSecondStateMachineState,
        TEvent, TContext>
        : StateMachineBaseAsync<(TFirstStateMachineState first, TSecondStateMachineState second), TEvent, TContext>
        where TFirstStateMachine : IStateMachineAsync<TFirstStateMachineState, TEvent, TContext>
        where TSecondStateMachine : IStateMachineAsync<TSecondStateMachineState, TEvent, TContext>
        where TEvent : IEquatable<TEvent>
    {
        /// <summary>
        /// Gets the first state machine
        /// </summary>
        protected TFirstStateMachine firstStateMachine;

        /// <summary>
        /// Gets the second state machine
        /// </summary>
        protected TSecondStateMachine secondStateMachine;

        /// <summary>
        /// Creates a new <see cref="CompoundStateMachineAsync{TFirstStateMachine, TSecondStateMachine, TFirstStateMachineState, TSecondStateMachineState, TEvent, TContext}"/>
        /// </summary>
        /// <remarks>
        /// Starts with a state equal to the two state machines combined state
        /// </remarks>
        public CompoundStateMachineAsync(TFirstStateMachine firstStateMachine, TSecondStateMachine secondStateMachine)
            : base((firstStateMachine.CurrentState, secondStateMachine.CurrentState))
        {
            this.firstStateMachine = firstStateMachine;
            this.secondStateMachine = secondStateMachine;

            this.firstStateMachine.StateChanges += FirstStateMachine_StateChanges;
            this.secondStateMachine.StateChanges += SecondStateMachine_StateChanges;

            this.CurrentState = (firstStateMachine.CurrentState, secondStateMachine.CurrentState);
        }

        private void FirstStateMachine_StateChanges(StateTransitionArgs<TContext, TFirstStateMachineState> transitionArgs)
        {
            var oldState = this.CurrentState;
            var newState = (this.firstStateMachine.CurrentState, this.secondStateMachine.CurrentState);
            this.OnStateChanging(transitionArgs.Context, oldState, newState);
            this.CurrentState = newState;
            this.OnStateChanged(transitionArgs.Context, oldState, this.CurrentState);
        }

        private void SecondStateMachine_StateChanges(StateTransitionArgs<TContext, TSecondStateMachineState> transitionArgs)
        {
            var oldState = this.CurrentState;
            this.CurrentState = (this.firstStateMachine.CurrentState, this.secondStateMachine.CurrentState);
            this.OnStateChanged(transitionArgs.Context, oldState, this.CurrentState);
        }

        /// <summary>
        /// Tick on both state machines
        /// </summary>
        public override async Task Tick(DateTimeOffset now, TContext context, int limitOnNumberExecuted = 10000)
        {
            await base.Tick(now, context, limitOnNumberExecuted);
            await this.firstStateMachine.Tick(now, context, limitOnNumberExecuted);
            await this.secondStateMachine.Tick(now, context, limitOnNumberExecuted);
        }

        /// <summary>
        /// An event happens, transition both state machines
        /// </summary>
        public override async Task EventHappens(TEvent @event, TContext context)
        {
            await this.firstStateMachine.EventHappens(@event, context);
            await this.secondStateMachine.EventHappens(@event, context);
            this.OnEventHappens(context, @event);
        }

        // Use C#7 Tuple Syntax to enable (On, Occupied).When(event, ...) ??
    }
}