using Abodit.StateMachine;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    [Serializable]
    public class FridgeStateMachine : CompoundStateMachineAsync<CoolingStateMachine, OpenClosedStateMachine, CoolingStateMachine.State, OpenClosedStateMachine.State, Event, IFridge>
    {
        protected override void OnStateChanging(IFridge context, (StateMachineAsync<CoolingStateMachine, Event, IFridge>.State first, StateMachineAsync<OpenClosedStateMachine, Event, IFridge>.State second) oldState, (StateMachineAsync<CoolingStateMachine, Event, IFridge>.State first, StateMachineAsync<OpenClosedStateMachine, Event, IFridge>.State second) newState)
        {
            Trace.WriteLine("Transitioning to state " + newState);
        }

        protected override void OnStateChanged(IFridge context, (StateMachineAsync<CoolingStateMachine, Event, IFridge>.State first, StateMachineAsync<OpenClosedStateMachine, Event, IFridge>.State second) oldState, (StateMachineAsync<CoolingStateMachine, Event, IFridge>.State first, StateMachineAsync<OpenClosedStateMachine, Event, IFridge>.State second) newState)
        {
            Trace.WriteLine("Transitioned to state " + newState);
        }

        /// <summary>
        /// Default constructor - used only when deserializing (does not start the clock)
        /// </summary>
        public FridgeStateMachine()
            : base(new CoolingStateMachine(), new OpenClosedStateMachine())
        {
        }

        public async Task TemperatureChanges(IFridge fridge, decimal temperature)
        {
            await this.firstStateMachine.TemperatureChanges(fridge, temperature);
        }

        internal async Task DoorOpens(IFridge fridge)
        {
            await this.firstStateMachine.EventHappens(Fridge.eDoorOpens, fridge);
            await this.secondStateMachine.EventHappens(Fridge.eDoorOpens, fridge);
        }

        internal async Task DoorCloses(IFridge fridge)
        {
            await this.firstStateMachine.EventHappens(Fridge.eDoorCloses, fridge);
            await this.secondStateMachine.EventHappens(Fridge.eDoorCloses, fridge);
        }

        internal CoolingStateMachine CoolingStateMachine => this.firstStateMachine;
        internal OpenClosedStateMachine OpenClosedStateMachine => this.secondStateMachine;
    }
}