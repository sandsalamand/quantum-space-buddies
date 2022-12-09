using QSB.Utility;
using System;
using System.Collections.Generic;

namespace QSB.Messaging;

internal static class QSBMessageSubscriptionManager
{
	private static Dictionary<Type, MessageSubscribers> subscribers = new Dictionary<Type, MessageSubscribers>();
	internal const int EmptyHashCode = -111111;

	private class MessageSubscribers
	{
		//int is object hash, used for unsubscribing
		public Dictionary<int, List<Action<QSBMessage>>> subscribedLocalActions = new Dictionary<int, List<Action<QSBMessage>>>();
		public Dictionary<int, List<Action<QSBMessage>>> subscribedRemoteActions = new Dictionary<int, List<Action<QSBMessage>>>();

		public MessageSubscribers(int objHash, Action<QSBMessage> action) => subscribedLocalActions.Add(objHash, new List<Action<QSBMessage>> { action });
		public MessageSubscribers(int objHash, List<Action<QSBMessage>> actions) => subscribedLocalActions.Add(objHash, actions);

		public void AddToActions(int objHash, MessageInvokeCondition msgInvokeCondition, Action<QSBMessage> action)
		{
			var actions = msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal ? subscribedLocalActions : subscribedRemoteActions;
			if (actions.ContainsKey(objHash))
				actions[objHash].Add(action);
			else
				actions.Add(objHash, new List<Action<QSBMessage>> { action });
		}

		public void RemoveFromActions(int objHash, MessageInvokeCondition msgInvokeCondition)
		{
			var actions = msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal ? subscribedLocalActions : subscribedRemoteActions;
			if (actions.ContainsKey(objHash))
				actions[objHash].Clear();
		}
	}

	internal static void Subscribe<M>(int objHash, MessageInvokeCondition msgInvokeCondition, Action<M> action) where M : QSBMessage
	{
		Type messageType = typeof(M);
		if (subscribers.ContainsKey(messageType))
			subscribers[messageType].AddToActions(objHash, msgInvokeCondition, (Action<QSBMessage>)action);
		else
			subscribers.Add(messageType, new MessageSubscribers(objHash, (Action<QSBMessage>)action));
	}

	internal static void Unsubscribe<M>(int objHash, MessageInvokeCondition msgInvokeCondition) where M : QSBMessage
	{
		if (subscribers.ContainsKey(typeof(M)))
			subscribers[typeof(M)].RemoveFromActions(objHash, msgInvokeCondition);
	}

	internal static void InvokeSubscribers<M>(M qsbMesssage, MessageInvokeCondition msgInvokeCondition) where M : QSBMessage
	{
		if (subscribers.ContainsKey(typeof(M)))
		{
			MessageSubscribers msgSubscribers = subscribers[typeof(M)];
			var actionsToInvoke = (msgInvokeCondition == MessageInvokeCondition.OnReceiveLocal) ? msgSubscribers.subscribedLocalActions : msgSubscribers.subscribedRemoteActions;
			actionsToInvoke.Values?.ForEach((actions) => actions.ForEach((action) => action?.Invoke(qsbMesssage)));
		}
	}
}

public enum MessageInvokeCondition
{
	OnReceiveLocal,
	OnReceiveRemote
}

