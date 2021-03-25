using Abodit.StateMachine;
using System;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    /// <summary>
    /// An Open/Closed state machine.
    /// Simplest possible state machine.
    /// Two states flips between them.
    /// Entry and exit actions could, for example, turn the light on and off
    /// </summary>

    public class OpenClosedStateMachine : StateMachineAsync<OpenClosedStateMachine, Event, IFridge>
    {
        public static readonly State Open = AddState("Open", (m, s, e, f) =>
        {
            f.Open = true;
            return Task.CompletedTask;
        });

        public static readonly State Closed = AddState("Closed", (m, s, e, f) =>
        {
            f.Open = false;
            return Task.CompletedTask;
        });

        static OpenClosedStateMachine()
        {
            Closed
                .When(Fridge.eDoorOpens, (m, s, e, c) => Task.FromResult(Open));

            Open
                .When(Fridge.eDoorCloses, (m, s, e, c) => Task.FromResult(Closed));
        }

        public OpenClosedStateMachine()
            : base(Closed)
        {
        }
    }
}