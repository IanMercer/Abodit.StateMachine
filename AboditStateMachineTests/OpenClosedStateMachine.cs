using Abodit.StateMachine;
using System;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    // Open / Closed state machine is much simpler, for now, just tracks state, turns light on and off

    [Serializable]
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