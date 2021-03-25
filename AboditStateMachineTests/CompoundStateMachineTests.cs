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
                coolingStateMachine.CurrentState.Should().Be(CoolingStateMachine.Idle);
                await coolingStateMachine.TemperatureChanges(fridge, 40);
                coolingStateMachine.CurrentState.Should().Be(CoolingStateMachine.Cooling);
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
                openClosedStateMachine.CurrentState.Should().Be(OpenClosedStateMachine.Closed);
                await fridge.DoorCloses();
                openClosedStateMachine.CurrentState.Should().Be(OpenClosedStateMachine.Closed);
                await fridge.DoorOpens();
                openClosedStateMachine.CurrentState.Should().Be(OpenClosedStateMachine.Open);
                await fridge.DoorCloses();
                openClosedStateMachine.CurrentState.Should().Be(OpenClosedStateMachine.Closed);
            }
        }
    }

    [TestClass]
    public class CompoundStateMachineTests
    {
        private readonly ManualTimeProvider timeProvider = new ManualTimeProvider();
        private readonly Fridge fridge = new Fridge { SetTemperature = 35, ActualTemperature = 34 };

        [TestMethod]
        public async Task CanOperateFridge()
        {
            using (TimeProvider.StartUsing(timeProvider))
            {
                fridge.StateString.Should().Be("Closed Idle Light off");
                await fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Closed Cooling Light off");

                await fridge.TemperatureChanges(33);
                fridge.StateString.Should().Be("Closed Idle Light off");

                await fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Idle Light on");

                await fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Open Idle Light on");

                await fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Idle Light off");

                timeProvider.Add(minutes: 5);
                await fridge.Tick();
                fridge.StateString.Should().Be("Closed Cooling Light off");

                await fridge.DoorOpens();
                timeProvider.Add(minutes: 5);
                await fridge.Tick();
                await fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Idle Light off");

                await fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Closed Cooling Light off");

                await fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Idle Light on");

                timeProvider.Add(minutes: 60);
                await fridge.Tick();
                fridge.StateString.Should().Be("Open Defrosting Light on");

                timeProvider.Add(minutes: 5);
                await fridge.Tick();
                fridge.StateString.Should().Be("Open Cooling Light on");

                await fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Cooling Light off");

                timeProvider.Add(minutes: 60);
                await fridge.Tick();
                fridge.StateString.Should().Be("Closed Defrosting Light off");

                await fridge.DoorOpens();
                fridge.StateString.Should().Be("Open Defrosting Light on");

                await fridge.TemperatureChanges(40);
                fridge.StateString.Should().Be("Open Defrosting Light on");

                await fridge.DoorCloses();
                fridge.StateString.Should().Be("Closed Defrosting Light off");

                await fridge.TemperatureChanges(33);
                fridge.StateString.Should().Be("Closed Defrosting Light off");
            }
        }
    }
}