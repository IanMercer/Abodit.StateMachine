using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Abodit.StateMachine
{
    public abstract partial class StateMachineAsync<TStateMachine, TEvent, TContext>
        : IStateMachineAsync<StateMachineAsync<TStateMachine, TEvent, TContext>.State, TEvent, TContext>
    {
        /// <summary>
        /// The static State Definitions that are created for a StateMachine
        /// </summary>
        [DebuggerDisplay("StateDefinition = {Name}")]
        private class StateDefinition
        {
            /// <summary>
            /// Get a State object from a StateDef
            /// </summary>
            /// <returns></returns>
            public State GetState()
            {
                return new State(this.Name);
            }

            /// <summary>
            /// The Name of this state definition
            /// </summary>
            /// <remarks>
            /// Used for serialization of the state machine, also handy for debugging
            /// </remarks>
            [XmlAttribute]
            public string Name { get; set; }

            internal StateDefinition? Parent { get; private set; } = null;

            /// <summary>
            /// Get the ancestor states in ascending order, top-most state last
            /// </summary>
            internal IEnumerable<StateDefinition> SelfAndAncestorsInAscendingOrder
            {
                get
                {
                    var p = this;
                    while (p != null)
                    {
                        yield return p;
                        p = p.Parent;
                    }
                }
            }

            /// <summary>
            /// Get the ancestor states in ascending order, top-most state last
            /// </summary>
            internal IEnumerable<StateDefinition> AncestorsInAscendingOrder
            {
                get
                {
                    var p = this.Parent;
                    while (p != null)
                    {
                        yield return p;
                        p = p.Parent;
                    }
                }
            }

            internal Func<TStateMachine, TEvent, State, TContext, Task>? EntryAction = null;

            internal Func<TStateMachine, State, TEvent, TContext, Task>? ExitAction = null;

            // Entry and exit actions are looked up by NAME so that you can serialize and deserialize a State
            // and all we care about is the name of the state as the entry and exit actions were created
            // statically when the state machine class was initialized

            internal readonly IDictionary<TEvent, Func<TStateMachine, State, TEvent, TContext, Task<State>>> transitions =
                new Dictionary<TEvent, Func<TStateMachine, State, TEvent, TContext, Task<State>>>();

            /// <summary>
            /// Create a new State with a name and an optional entry and exit action
            /// </summary>
            public StateDefinition(string name,
                                    Func<TStateMachine, TEvent, State, TContext, Task>? entryAction = null,
                                    Func<TStateMachine, State, TEvent, TContext, Task>? exitAction = null,
                                    State? parent = null)
            {
                this.Name = name;
                if (parent is State)
                {
                    // Map the parent to the identity mapped StateDefinition by name
                    this.Parent = definitions[parent.Name];
                }
                this.EntryAction = entryAction;
                this.ExitAction = exitAction;
            }

            public StateDefinition When(TEvent @event, Func<TStateMachine, State, TEvent, TContext, Task<State>> action)
            {
                transitions.Add(@event, action);
                return this;
            }

            public override string ToString()
            {
                return "*" + this.Name + "*";
            }
        }
    }
}