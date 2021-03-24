using Abodit.StateMachine;
using System;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    /// <summary>
    /// Cooling -> Idle -> Defrosting etc.
    ///
    /// When the door opens stops cooling, starts again after its been closed for a while.
    /// Defrosts regularly.
    ///
    /// </summary>
    public class CoolingStateMachine : StateMachineAsync<CoolingStateMachine, Event, IFridge>
    {
        public static readonly State Cooling = AddState("Cooling",
            (m, e, s, f) => { f.Cooling = true; return Task.CompletedTask; },
            (m, e, s, f) => { f.Cooling = false; return Task.CompletedTask; });

        public static readonly State Idle = AddState("Idle");

        public static readonly State Defrosting = AddState("Defrosting",
            (m, e, s, f) =>
            {
                f.Defrosting = true;
                m.After(TimeSpan.FromMinutes(5), eFinishDefrosting);
                return Task.CompletedTask;
            },
            (m, e, s, f) =>
            {
                f.Defrosting = false;
                return Task.CompletedTask;
            }
            );

        public async Task TemperatureChanges(IFridge fridge, decimal temperature)
        {
            fridge.ActualTemperature = temperature;
            await this.EventHappens(Fridge.eTemperatureChanges, fridge);
        }

        // TODO: Add a way to change the SetTemperature

        private static Event eDoorLeftOpen = new Event("Door left open");
        private static Event eDoorStaysClosed = new Event("Door stays closed");
        private static Event eFinishDefrosting = new Event("Finish Defrosting");
        private static Event eStartDefrost = new Event("Start Defrost");

        static CoolingStateMachine()
        {
            Idle
                .When(Fridge.eTemperatureChanges, (m, s, e, c) =>
                    Task.FromResult(c.Open ? Idle : c.ActualTemperature > c.SetTemperature ? Cooling : Idle))

                .When(Fridge.eDoorCloses, (m, s, e, c) =>
                {
                    m.After(TimeSpan.FromSeconds(10), eDoorStaysClosed);
                    return Task.FromResult(s);
                })

                .When(Fridge.eDoorOpens, (m, s, e, c) =>
                    { m.CancelScheduledEvent(eDoorStaysClosed); return Task.FromResult(s); })
                // Comes on only after 10s with door closed

                .When(eDoorStaysClosed, (m, s, e, c) =>
                    Task.FromResult(c.ActualTemperature > c.SetTemperature ? Cooling : Idle))

                .When(eStartDefrost, Defrosting);

            Defrosting
                .When(eFinishDefrosting, Cooling);

            Cooling
                // Goes off immediately when you open the door
                .When(Fridge.eDoorOpens, (m, s, e, c) =>
                    Task.FromResult(Idle))

                .When(Fridge.eTemperatureChanges, (m, s, e, c) =>
                    Task.FromResult(c.ActualTemperature > c.SetTemperature ? s : Idle))

                .When(eStartDefrost, Defrosting);
        }

        public CoolingStateMachine() : base(Idle)
        {
            this.Every(TimeSpan.FromHours(1), eStartDefrost);
        }
    }
}