using OWML.ModHelper.Events;
using QSB.Player.Messages;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QSB.Messaging;

internal static class QSBMessageSubscriptionManager
{
	private static Dictionary<Type, MessageSubscribers> subscribers = new Dictionary<Type, MessageSubscribers>();

	private class MessageSubscribers
	{
		//int is object hash, used for unsubscribing
		public Dictionary<int, List<Action>> subscribedLocalActions = new Dictionary<int, List<Action>>();
		public Dictionary<int, List<Action>> subscribedRemoteActions = new Dictionary<int, List<Action>>();

		public MessageSubscribers(int objHash, Action action) => subscribedLocalActions.Add(objHash, new List<Action> { action });
		public MessageSubscribers(int objHash, List<Action> actions) => subscribedLocalActions.Add(objHash, actions);

		public void AddToActions(int objHash, MessageInvokeCondition msgInvokeCondition, Action action)
		{
			var actions = msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal ? subscribedLocalActions : subscribedRemoteActions;
			if (actions.ContainsKey(objHash))
				actions[objHash].Add(action);
			else
				actions.Add(objHash, new List<Action> { action });
		}

		public void RemoveFromActions(int objHash, MessageInvokeCondition msgInvokeCondition)
		{
			var actions = msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal ? subscribedLocalActions : subscribedRemoteActions;
			if (actions.ContainsKey(objHash))
				actions[objHash].Clear();
		}
	}

	internal static void Subscribe<M>(int objHash, MessageInvokeCondition msgInvokeCondition, Action action) where M : QSBMessage
	{
		Type messageType = typeof(M);
		if (subscribers.ContainsKey(messageType))
			subscribers[messageType].AddToActions(objHash, msgInvokeCondition, action);
		else
			subscribers.Add(messageType, new MessageSubscribers(objHash, action));
	}

	internal static void Unsubscribe<M>(int objHash, MessageInvokeCondition msgInvokeCondition) where M : QSBMessage
	{
		if (subscribers.ContainsKey(typeof(M)))
			subscribers[typeof(M)].RemoveFromActions(objHash, msgInvokeCondition);
	}

	internal static void InvokeSubscribers<M>(M QSBMessage, MessageInvokeCondition msgInvokeCondition) where M : QSBMessage
	{
		if (subscribers.ContainsKey(typeof(M)))
		{
			MessageSubscribers msgSubscribers = subscribers[typeof(M)];
			var actionsToInvoke = (msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal) ? msgSubscribers.subscribedLocalActions : msgSubscribers.subscribedRemoteActions;
			actionsToInvoke.Values?.ForEach((actions) => actions.ForEach((action) => action?.Invoke()));
		}
	}
}

public enum MessageInvokeCondition
{
	OnReceiveLocal,
	OnReceiveRemote
}

