using Abodit.StateMachine;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace AboditStateMachineTests
{
    [TestClass]
    public class CoolingStateMachineTests
    {
        private readonly ManualTimeProvider timeProvider = new ManualTimeProvider();
        private readonly Fridge fridge = new Fridge();

        [TestMethod]
        public async Task CoolingStateMachineBehaves()
        {
            var coolingStateMachine = fridge.CoolingStateMachine;

            using (TimeProvider.StartUsing(timeProvider))
            {
                coolingStateMachine.CurrentState?.ToString().Should().Be("*Idle*");
                await coolingStateMachine.TemperatureChanges(fridge, 40);
                coolingStateMachine.CurrentState?.ToString().Should().Be("*Cooling*");

                // TODO: Add a way to change the SET TEMPERATURE and test it too
            }
        }
    }

    [TestClass]
    public class OpenClosedMachineTests
    {
        private readonly ManualTimeProvider timeProvider = new ManualTimeProvider();
        private readonly Fridge fridge = new Fridge();

        [TestMethod]
        public async Task OpenClosedStateMachineBehaves()
        {
            var openClosedStateMachine = fridge.OpenClosedStateMachine;

            using (TimeProvider.StartUsing(timeProvider))
            {
                openClosedStateMachine.CurrentState?.ToString().Should().Be("*Closed*");
                await fridge.DoorCloses();
                openClosedStateMachine.CurrentState?.ToString().Should().Be("*Closed*");
                await fridge.DoorOpens();
                openClosedStateMachine.CurrentState?.ToString().Should().Be("*Open*");
                await fridge.DoorCloses();
                openClosedStateMachine.CurrentState?.ToString().Should().Be("*Closed*");
            }
        }
    }

    [TestClass]
    public class CompoundStateMachineTests
    {
        private readonly ManualTimeProvider timeProvider = new ManualTimeProvider();
        private readonly Fridge fridge = new Fridge { SetTemperature = 35, ActualTemperature = 34 };

        [TestMethod]
        public void CanOperateFridge()
        {
            using (TimeProvider.StartUsing(timeProvider))
            {
                fridge.StateString.Should().Be("Closed Idle Light off");
                fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Closed Cooling Light off");

                fridge.TemperatureChanges(33);
                fridge.StateString.Should().Be("Closed Idle Light off");

                fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Idle Light on");

                fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Open Idle Light on");

                fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Idle Light off");

                timeProvider.Add(minutes: 5);
                fridge.Tick();
                fridge.StateString.Should().Be("Closed Cooling Light off");

                fridge.DoorOpens();
                timeProvider.Add(minutes: 5);
                fridge.Tick();
                fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Idle Light off");

                fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Closed Cooling Light off");

                fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Idle Light on");

                timeProvider.Add(minutes: 60);
                fridge.Tick();
                fridge.StateString.Should().Be("Open Defrosting Light on");

                timeProvider.Add(minutes: 5);
                fridge.Tick();
                fridge.StateString.Should().Be("Open Cooling Light on");

                fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Cooling Light off");

                timeProvider.Add(minutes: 60);
                fridge.Tick();
                fridge.StateString.Should().Be("Closed Defrosting Light off");

                fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Defrosting Light on");

                fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Open Defrosting Light on");

                fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Defrosting Light off");

                fridge.TemperatureChanges(33);
                fridge.StateString.Should().Be("Closed Defrosting Light off");
            }
        }
    }
}