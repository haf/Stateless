using System;
using NUnit.Framework;

namespace Stateless.Tests
{
	public class StateMachineCloneTestFixture
	{
		private StateMachine<State, Trigger> _stateMachine;

		[SetUp] 
		public void Given()
		{
			_stateMachine = new StateMachine<State, Trigger>(State.B);
			_stateMachine.Configure(State.B).Permit(Trigger.X, State.C);

			// sanity check:
			Assert.That(_stateMachine.IsInState(State.B));
		}

		[TearDown]
		public void Finally()
		{
			_stateMachine = null;
		}

		[Test]
		public void It_can_be_cloned()
		{
			var clone = _stateMachine.Clone();
			Assert.That(clone, Is.InstanceOfType(typeof (StateMachine)));
			Assert.That(clone, Is.Not.SameAs(_stateMachine));
			Assert.That(clone.Id, Is.Not.EqualTo(_stateMachine.Id));
		}

		[Test]
		public void Clone_has_same_state()
		{
			Assert.That(
				_stateMachine.Clone().State,
				Is.EqualTo(State.B));
		}

		[Test]
		public void Clone_fires_previous_callbacks()
		{
			// given:
			bool firedOriginal = false;
			_stateMachine.Configure(State.B)
				.OnExit(() => firedOriginal = true);

			// when
			var clone = _stateMachine.Clone();

			//then:
			clone.Fire(Trigger.X);

			Assert.That(firedOriginal, Is.True, "because it should fire the previous handlers, also");
		}

		[Test]
		public void Parent_doesnt_fire_new_callbacks_in_clone()
		{
			var clone = _stateMachine.Clone();

			bool cloneFired = false,
				 parentFired = false;

			clone.Configure(State.B).OnExit(() => cloneFired = true);
			_stateMachine.Configure(State.B).OnExit(() => parentFired = true);
			
			clone.Fire(Trigger.X);

			Assert.That(cloneFired, "Clone should have fired");
			Assert.That(parentFired, Is.False, "parent should not fire");
		}

		[Test]
		public void Clone_doesnt_fire_new_callbacks_in_parent()
		{
			var clone = _stateMachine.Clone();

			bool cloneFired = false,
				 parentFired = false;

			clone.Configure(State.B).OnExit(() => cloneFired = true);
			_stateMachine.Configure(State.B).OnExit(() => parentFired = true);

			_stateMachine.Fire(Trigger.X);

			Assert.That(cloneFired, Is.False, "because the callback was registered after the clone was made");
		}

		[Test]
		public void Callback_on_invalid_can_be_updated_without_interfering_with_parent()
		{
			bool parentFired = false,
			     cloneFired = false;
			
			_stateMachine.OnUnhandledTrigger((s, t) => parentFired = true);

			var clone = _stateMachine.Clone();
			clone.OnUnhandledTrigger((s,t) => cloneFired = true);

			clone.Fire(Trigger.Y);

			Assert.That(cloneFired, "because it was updated with a new callback");
			Assert.That(parentFired, Is.False, "because it was overwritten in the clone");

			_stateMachine.Fire(Trigger.Y);

			Assert.That(parentFired, "because this time we fired on the parent");
		}
	}
}