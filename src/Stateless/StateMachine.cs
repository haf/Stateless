using System;
using System.Collections.Generic;
using System.Linq;

namespace Stateless
{
	/// <summary>
	/// 	Base class for all state machines
	/// </summary>
	public abstract class StateMachine
	{
		private readonly Guid _id;

		/// <summary>
		/// 	Initializes the ID
		/// </summary>
		protected StateMachine()
		{
			_id = Guid.NewGuid();
		}

		/// <summary>
		/// 	A unique identifier for this state machine.
		/// </summary>
		public Guid Id
		{
			get { return _id; }
		}
	}

	/// <summary>
	/// 	Models behaviour as transitions between a finite set of states.
	/// </summary>
	/// <typeparam name = "TState">The type used to represent the states.</typeparam>
	/// <typeparam name = "TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
	public partial class StateMachine<TState, TTrigger> : StateMachine, ICloneable
	{
		private readonly IDictionary<TState, StateRepresentation> _stateConfiguration =
			new Dictionary<TState, StateRepresentation>();

		private readonly IDictionary<TTrigger, TriggerWithParameters> _triggerConfiguration =
			new Dictionary<TTrigger, TriggerWithParameters>();

		private readonly Func<TState> _stateAccessor;
		private readonly Action<TState> _stateMutator;
		private Action<TState, TTrigger> _unhandledTriggerAction = DefaultUnhandledTriggerAction;

		private StateMachine(Func<TState> stateAccessor,
		                     IEnumerable<KeyValuePair<TState, StateRepresentation>> stateConfiguration,
		                     IEnumerable<KeyValuePair<TTrigger, TriggerWithParameters>> triggerConfiguration)
		{
			var existingState = stateAccessor();
			var reference = new StateReference() {State = existingState};
			_stateAccessor = () => reference.State;
			_stateMutator = s => reference.State = s;

			foreach (var stateRepresentation in stateConfiguration)
				_stateConfiguration.Add(stateRepresentation.Key, stateRepresentation.Value.Clone());

			foreach (var triggerWithParameters in triggerConfiguration)
				_triggerConfiguration.Add(triggerWithParameters.Key, triggerWithParameters.Value.Clone());
		}

		/// <summary>
		/// 	Construct a state machine with external state storage.
		/// </summary>
		/// <param name = "stateAccessor">A function that will be called to read the current state value.</param>
		/// <param name = "stateMutator">An action that will be called to write new state values.</param>
		public StateMachine(Func<TState> stateAccessor, Action<TState> stateMutator)
		{
			_stateAccessor = Enforce.ArgumentNotNull(stateAccessor, "stateAccessor");
			_stateMutator = Enforce.ArgumentNotNull(stateMutator, "stateMutator");
		}

		/// <summary>
		/// 	Construct a state machine.
		/// </summary>
		/// <param name = "initialState">The initial state.</param>
		public StateMachine(TState initialState)
		{
			var reference = new StateReference {State = initialState};
			_stateAccessor = () => reference.State;
			_stateMutator = s => reference.State = s;
		}

		/// <summary>
		/// 	The current state.
		/// </summary>
		public TState State
		{
			get { return _stateAccessor(); }
			private set { _stateMutator(value); }
		}

		/// <summary>
		/// 	The currently-permissible trigger values.
		/// </summary>
		public IEnumerable<TTrigger> PermittedTriggers
		{
			get { return CurrentRepresentation.PermittedTriggers; }
		}

		private StateRepresentation CurrentRepresentation
		{
			get { return GetRepresentation(State); }
		}

		private StateRepresentation GetRepresentation(TState state)
		{
			StateRepresentation result;

			if (!_stateConfiguration.TryGetValue(state, out result))
			{
				result = new StateRepresentation(state);
				_stateConfiguration.Add(state, result);
			}

			return result;
		}

		/// <summary>
		/// 	Begin configuration of the entry/exit actions and allowed transitions
		/// 	when the state machine is in a particular state.
		/// </summary>
		/// <param name = "state">The state to configure.</param>
		/// <returns>A configuration object through which the state can be configured.</returns>
		public StateConfiguration Configure(TState state)
		{
			return new StateConfiguration(GetRepresentation(state), GetRepresentation);
		}

		/// <summary>
		/// 	Transition from the current state via the specified trigger.
		/// 	The target state is determined by the configuration of the current state.
		/// 	Actions associated with leaving the current state and entering the new one
		/// 	will be invoked.
		/// </summary>
		/// <param name = "trigger">The trigger to fire.</param>
		/// <exception cref = "System.InvalidOperationException">The current state does
		/// 	not allow the trigger to be fired.</exception>
		public void Fire(TTrigger trigger)
		{
			InternalFire(trigger, new object[0]);
		}

		/// <summary>
		/// 	Transition from the current state via the specified trigger.
		/// 	The target state is determined by the configuration of the current state.
		/// 	Actions associated with leaving the current state and entering the new one
		/// 	will be invoked.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <param name = "trigger">The trigger to fire.</param>
		/// <param name = "arg0">The first argument.</param>
		/// <exception cref = "System.InvalidOperationException">The current state does
		/// 	not allow the trigger to be fired.</exception>
		public void Fire<TArg0>(TriggerWithParameters<TArg0> trigger, TArg0 arg0)
		{
			Enforce.ArgumentNotNull(trigger, "trigger");
			InternalFire(trigger.Trigger, arg0);
		}

		/// <summary>
		/// 	Transition from the current state via the specified trigger.
		/// 	The target state is determined by the configuration of the current state.
		/// 	Actions associated with leaving the current state and entering the new one
		/// 	will be invoked.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <typeparam name = "TArg1">Type of the second trigger argument.</typeparam>
		/// <param name = "arg0">The first argument.</param>
		/// <param name = "arg1">The second argument.</param>
		/// <param name = "trigger">The trigger to fire.</param>
		/// <exception cref = "System.InvalidOperationException">The current state does
		/// 	not allow the trigger to be fired.</exception>
		public void Fire<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, TArg0 arg0, TArg1 arg1)
		{
			Enforce.ArgumentNotNull(trigger, "trigger");
			InternalFire(trigger.Trigger, arg0, arg1);
		}

		/// <summary>
		/// 	Transition from the current state via the specified trigger.
		/// 	The target state is determined by the configuration of the current state.
		/// 	Actions associated with leaving the current state and entering the new one
		/// 	will be invoked.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <typeparam name = "TArg1">Type of the second trigger argument.</typeparam>
		/// <typeparam name = "TArg2">Type of the third trigger argument.</typeparam>
		/// <param name = "arg0">The first argument.</param>
		/// <param name = "arg1">The second argument.</param>
		/// <param name = "arg2">The third argument.</param>
		/// <param name = "trigger">The trigger to fire.</param>
		/// <exception cref = "System.InvalidOperationException">The current state does
		/// 	not allow the trigger to be fired.</exception>
		public void Fire<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, TArg0 arg0, TArg1 arg1,
		                                      TArg2 arg2)
		{
			Enforce.ArgumentNotNull(trigger, "trigger");
			InternalFire(trigger.Trigger, arg0, arg1, arg2);
		}

		private void InternalFire(TTrigger trigger, params object[] args)
		{
			TriggerWithParameters configuration;
			if (_triggerConfiguration.TryGetValue(trigger, out configuration))
				configuration.ValidateParameters(args);

			TriggerBehaviour triggerBehaviour;
			if (!CurrentRepresentation.TryFindHandler(trigger, out triggerBehaviour))
			{
				_unhandledTriggerAction(CurrentRepresentation.UnderlyingState, trigger);
				return;
			}

			var source = State;
			TState destination;
			if (triggerBehaviour.ResultsInTransitionFrom(source, args, out destination))
			{
				var transition = new Transition(source, destination, trigger);

				CurrentRepresentation.Exit(transition);
				State = transition.Destination;
				CurrentRepresentation.Enter(transition, args);
			}
		}

		/// <summary>
		/// 	Override the default behaviour of throwing an exception when an unhandled trigger
		/// 	is fired.
		/// </summary>
		/// <param name = "unhandledTriggerAction">An action to call when an unhandled trigger is fired.</param>
		public void OnUnhandledTrigger(Action<TState, TTrigger> unhandledTriggerAction)
		{
			if (unhandledTriggerAction == null) throw new ArgumentNullException("unhandledTriggerAction");
			_unhandledTriggerAction = unhandledTriggerAction;
		}

		/// <summary>
		/// 	Determine if the state machine is in the supplied state.
		/// </summary>
		/// <param name = "state">The state to test for.</param>
		/// <returns>True if the current state is equal to, or a substate of,
		/// 	the supplied state.</returns>
		public bool IsInState(TState state)
		{
			return CurrentRepresentation.IsIncludedIn(state);
		}

		/// <summary>
		/// 	Returns true if <paramref name = "trigger" /> can be fired
		/// 	in the current state.
		/// </summary>
		/// <param name = "trigger">Trigger to test.</param>
		/// <returns>True if the trigger can be fired, false otherwise.</returns>
		public bool CanFire(TTrigger trigger)
		{
			return CurrentRepresentation.CanHandle(trigger);
		}

		/// <summary>
		/// 	A human-readable representation of the state machine.
		/// </summary>
		/// <returns>A description of the current state and permitted triggers.</returns>
		public override string ToString()
		{
			return string.Format(
				"StateMachine {{ State = {0}, PermittedTriggers = {{ {1} }}}}",
				State,
				string.Join(", ", PermittedTriggers.Select(t => t.ToString()).ToArray()));
		}

		/// <summary>
		/// 	Specify the arguments that must be supplied when a specific trigger is fired.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <param name = "trigger">The underlying trigger value.</param>
		/// <returns>An object that can be passed to the Fire() method in order to 
		/// 	fire the parameterised trigger.</returns>
		public TriggerWithParameters<TArg0> SetTriggerParameters<TArg0>(TTrigger trigger)
		{
			var configuration = new TriggerWithParameters<TArg0>(trigger);
			SaveTriggerConfiguration(configuration);
			return configuration;
		}

		/// <summary>
		/// 	Specify the arguments that must be supplied when a specific trigger is fired.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <typeparam name = "TArg1">Type of the second trigger argument.</typeparam>
		/// <param name = "trigger">The underlying trigger value.</param>
		/// <returns>An object that can be passed to the Fire() method in order to 
		/// 	fire the parameterised trigger.</returns>
		public TriggerWithParameters<TArg0, TArg1> SetTriggerParameters<TArg0, TArg1>(TTrigger trigger)
		{
			var configuration = new TriggerWithParameters<TArg0, TArg1>(trigger);
			SaveTriggerConfiguration(configuration);
			return configuration;
		}

		/// <summary>
		/// 	Specify the arguments that must be supplied when a specific trigger is fired.
		/// </summary>
		/// <typeparam name = "TArg0">Type of the first trigger argument.</typeparam>
		/// <typeparam name = "TArg1">Type of the second trigger argument.</typeparam>
		/// <typeparam name = "TArg2">Type of the third trigger argument.</typeparam>
		/// <param name = "trigger">The underlying trigger value.</param>
		/// <returns>An object that can be passed to the Fire() method in order to 
		/// 	fire the parameterised trigger.</returns>
		public TriggerWithParameters<TArg0, TArg1, TArg2> SetTriggerParameters<TArg0, TArg1, TArg2>(TTrigger trigger)
		{
			var configuration = new TriggerWithParameters<TArg0, TArg1, TArg2>(trigger);
			SaveTriggerConfiguration(configuration);
			return configuration;
		}

		private void SaveTriggerConfiguration(TriggerWithParameters trigger)
		{
			if (_triggerConfiguration.ContainsKey(trigger.Trigger))
				throw new InvalidOperationException(
					string.Format(StateMachineResources.CannotReconfigureParameters, trigger));

			_triggerConfiguration.Add(trigger.Trigger, trigger);
		}

		private static void DefaultUnhandledTriggerAction(TState state, TTrigger trigger)
		{
			throw new InvalidOperationException(
				string.Format(
					StateMachineResources.NoTransitionsPermitted,
					trigger, state));
		}

		/// <summary>
		/// 	Creates a clone of the state machine which is 
		/// 	in the same state with the same callbacks, but with a new Id
		/// 	and new mutable data structures.
		/// </summary>
		/// <returns></returns>
		/// <exception cref = "NotImplementedException"></exception>
		public StateMachine<TState, TTrigger> Clone()
		{
			return new StateMachine<TState, TTrigger>(_stateAccessor, _stateConfiguration, _triggerConfiguration);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}