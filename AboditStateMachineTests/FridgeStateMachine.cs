using Abodit.StateMachine;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    /// <summary>
    /// The Fridge state machine is a compound state machine: the combination of an open/closed state machine
    /// for the door and a state machine for the chiller.
    /// </summary>
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

        /// <summary>
        /// Temperature changes
        /// </summary>
        /// <param name="fridge"></param>
        /// <param name="temperature"></param>
        /// <returns></returns>
        public async Task TemperatureChanges(IFridge fridge, decimal temperature)
        {
            fridge.ActualTemperature = temperature;
            await this.EventHappens(Fridge.eTemperatureChanges, fridge);
            await this.firstStateMachine.TemperatureChanges(fridge, temperature);
        }

        internal async Task DoorOpens(IFridge fridge)
        {
            await this.EventHappens(Fridge.eDoorOpens, fridge);
        }

        internal async Task DoorCloses(IFridge fridge)
        {
            await this.EventHappens(Fridge.eDoorCloses, fridge);
        }

        internal CoolingStateMachine CoolingStateMachine => this.firstStateMachine;

        internal OpenClosedStateMachine OpenClosedStateMachine => this.secondStateMachine;
    }
}