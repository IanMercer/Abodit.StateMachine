using Abodit.StateMachine;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    public interface ICoolingDevice
    {
        bool Cooling { get; set; }
        decimal SetTemperature { get; set; }
        decimal ActualTemperature { get; set; }
    }

    public interface IDoorDevice
    {
        bool Open { get; set; }
    }

    public interface IFridge : ICoolingDevice, IDoorDevice
    {
        bool Defrosting { get; set; }
    }

    /// <summary>
    /// A fridge has a compound state machine that represents the chiller, the door and the light
    /// </summary>
    public class Fridge : IFridge
    {
        public readonly FridgeStateMachine StateMachine = new FridgeStateMachine();

        public decimal SetTemperature { get; set; }
        public decimal ActualTemperature { get; set; }

        // State machine controls this
        public bool Open { get; set; }

        // State machine controls this
        public bool Cooling { get; set; }

        // State machine controls this
        public bool Defrosting { get; set; }

        internal async Task DoorOpens()
        {
            await this.StateMachine.DoorOpens(this);
            this.Light = true;
        }

        internal async Task DoorCloses()
        {
            await this.StateMachine.DoorCloses(this);
            this.Light = false;
        }

        internal async Task TemperatureChanges(decimal temperature)
        {
            await this.StateMachine.TemperatureChanges(this, temperature);
        }

        // State machine controls this
        public bool Light { get; set; }

        // Events shared between state machines for Fridge
        public static Event eTemperatureChanges = new Event("Temperature Changes");

        internal async Task Tick()
        {
            await this.StateMachine.Tick(TimeProvider.Current.Now, this);
        }

        public static Event eDoorCloses = new Event("Door closes");
        public static Event eDoorOpens = new Event("Door opens");

        public string StateString => (Open ? "Open" : "Closed") + " " +
            (Cooling ? "Cooling" : Defrosting ? "Defrosting" : "Idle") + " " +
            (Light ? "Light on" : "Light off");

        public CoolingStateMachine CoolingStateMachine => this.StateMachine.CoolingStateMachine;

        public OpenClosedStateMachine OpenClosedStateMachine => this.StateMachine.OpenClosedStateMachine;
    }
}