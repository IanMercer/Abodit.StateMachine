using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Abodit.StateMachine
{
    public abstract partial class StateMachineAsync<TStateMachine, TEvent, TContext>
    //        where TStateMachine : StateMachine<TStateMachine, TEvent, TContext>
    {
        /// <summary>
        /// A state that the state machine can be in
        /// </summary>
        /// <remarks>
        /// This is defined as a nested class to ensure each state machine has only the states that were defined for it
        /// and not for some other state machine. The State class delegates almost everything to the StateDefinition that
        /// is stored statically for each StateMachine.
        /// </remarks>
        [DebuggerDisplay("State = {Name}")]
        [Serializable]
        public class State : IEquatable<State> //, IState<TEvent, TContext>
        {
            /// <summary>
            /// The Name of this state (all states with the same Name are considered equal even if they are different objects)
            /// </summary>
            /// <remarks>
            /// This makes serialization and deserialization easier
            /// </remarks>
            [XmlAttribute]
            public string Name
            {
                get
                {
                    return this.stateDefinition.Name;
                }
                set
                {
                    this.stateDefinition = definitions[value];
                }
            }

            /// <summary>
            /// Our state definition stores everything we need to know - our entry and exit actions, our parentage, ...
            /// Every state with the same name within a StateMachine has the same definition
            /// </summary>
            [NonSerialized]
            private StateDefinition stateDefinition;

            /// <summary>
            /// States can optionally be organized hierarchically
            /// The hierarchy is set by the initial static creation of States
            /// not by some later states that are loaded from serialized versions
            /// </summary>
            public State? ParentState => stateDefinition.Parent?.GetState();

            /// <summary>
            /// Empty constuctor for serialization only
            /// </summary>
            public State()
            {
                this.stateDefinition = new StateDefinition("Undefined");
            }

            /// <summary>
            /// Create a new State with a name
            /// </summary>
            internal State(string name)
            {
                this.Name = name;
                this.stateDefinition = definitions[name];
            }

            /// <summary>
            /// Get a state by name, if it exists (suitable for use during deserialization from a string)
            /// </summary>
            public static bool TryGet(string name, out State state)
            {
                if (name is null) { state = null!; return false; }
                bool found = definitions.TryGetValue(name, out StateDefinition? sd);
                state = sd!.GetState()!;
                return found;
            }

            /// <summary>
            /// Act on an event, return the new state or null if there are no transitions possible (even inherited)
            /// </summary>
            public async Task<State?> OnEvent(TStateMachine stateMachine, TEvent @event, TContext context)
            {
                StateDefinition? selfOrAncestor = this.stateDefinition;
                int safety = 1000;              // just cautious code to ensure a bad data structure can't crash app
                while (selfOrAncestor != null && --safety > 0)
                {
                    if (selfOrAncestor.transitions.TryGetValue(@event, out Func<TStateMachine, State, TEvent, TContext, Task<State>>? transition))
                    {
                        // Execute the transition to get the new state
                        State newState = await transition!(stateMachine, this, @event, context);
                        if (newState != this)
                        {
                            // Entry and exit actions only fire when CHANGING state

                            // Exit actions happen from the innermost state to the outermost state
                            var oStates = this.stateDefinition.SelfAndAncestorsInAscendingOrder;
                            foreach (var n in oStates)
                            {
                                if (newState.Is(n.GetState())) break;      // Stop if we reach a common ancestor - we are NOT exiting that state
                                if (!(n.ExitAction is null))
                                    await n.ExitAction(stateMachine, newState, @event, context);
                            }

                            // Entry actions happen from the base-most state to the innermost state (like Constructors)
                            var nStates = newState.stateDefinition.SelfAndAncestorsInAscendingOrder.Reverse();
                            foreach (var n in nStates)
                            {
                                if (this.Is(n.GetState())) continue;       // If the old state is already a descendant of that state we don't enter it
                                if (!(n.EntryAction is null))
                                    await n.EntryAction(stateMachine, @event, newState, context);
                            }
                        }
                        return newState;
                    }
                    // otherwise, try parent, see if they have a transition we can use [inheritance]
                    selfOrAncestor = selfOrAncestor.Parent;
                }
                return null;
            }

            /// <summary>
            /// ToString
            /// </summary>
            public override string ToString()
            {
                return "*" + this.Name + "*";
            }

            /// <summary>
            /// Define what happens 'When' an event happens while in this state.
            /// </summary>
            public State When(TEvent @event, Func<TStateMachine, State, TEvent, TContext, Task<State>> action)
            {
                // Pass the When clause on to our underlying definition
                this.stateDefinition.When(@event, action);
                return this;
            }

            /// <summary>
            /// A simple form of When with no action (unless you defined actions for entering or leaving states)
            /// </summary>
            public State When(TEvent @event, State newState)
            {
                // Pass the When clause on to our underlying definition
                this.stateDefinition.When(@event, (m, s, e, c) => Task.FromResult(newState));
                return this;
            }

            // Because entry actions are executed automatically whenever an associated state is entered, they often determine the conditions of operation or the identity of the state, very much as a class constructor determines the identity of the object being constructed. For example, the identity of the "heating" state is determined by the fact that the heater is turned on. This condition must be established before entering any substate of "heating" because entry actions to a substate of "heating," like "toasting," rely on proper initialization of the "heating" superstate and perform only the differences from this initialization. Consequently, the order of execution of entry actions must always proceed from the outermost state to the innermost state (top-down).
            // Not surprisingly, this order is analogous to the order in which class constructors are invoked. Construction of a class always starts at the very root of the class hierarchy and follows through all inheritance levels down to the class being instantiated. The execution of exit actions, which corresponds to destructor invocation, proceeds in the exact reverse order (bottom-up).

            /// <summary>
            /// Add an action that happens when this state is entered
            /// </summary>
            /// <remarks>
            /// This is an alternative to setting it in the state constructor
            /// </remarks>
            public State OnEnter(Func<StateMachineAsync<TStateMachine, TEvent, TContext>, TEvent, State, TContext, Task> action)
            {
                this.stateDefinition.EntryAction = action;
                return this;
            }

            /// <summary>
            /// Add an action that happens when this state exits
            /// </summary>
            /// <remarks>
            /// This is an alternative to setting it in the state constructor
            /// </remarks>
            public State OnExit(Func<StateMachineAsync<TStateMachine, TEvent, TContext>, State, TEvent, TContext, Task> action)
            {
                this.stateDefinition.ExitAction = action;
                return this;
            }

            /// <summary>
            /// Test this state to see if it 'is' the other state, i.e. if it is the same or inherits from it
            /// </summary>
            public bool Is(State other)
            {
                return (this.Equals(other) || (this.ParentState?.Is(other) == true));
            }

            /// <summary>
            /// Gets the ObjectData for serialization
            /// </summary>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Name", this.Name);
            }

            // States are the same if ther underlying definition is the same

            /// <inheritdoc />
            public bool Equals(State? other)
            {
                if (other is null) return false;
                return this.stateDefinition == other.stateDefinition;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is State s && Equals(s);
            }

            /// <summary>
            /// Equals
            /// </summary>
            public static bool operator ==(State a, State b)
            {
                // If both are null, or both are same instance, return true.
                if (ReferenceEquals(a, b)) return true;
                if (ReferenceEquals(a, null)) return false;
                return a.Equals(b);
            }

            /// <summary>
            /// Not Equals
            /// </summary>
            public static bool operator !=(State a, State b)
            {
                return !(a == b);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return this.stateDefinition.GetHashCode();
            }
        }
    }
}