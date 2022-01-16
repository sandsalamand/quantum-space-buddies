﻿using QSB.Messaging;
using QSB.QuantumSync.WorldObjects;

namespace QSB.QuantumSync.Messages
{
	public class SetIsQuantumMessage : QSBBoolWorldObjectMessage<IQSBQuantumObject>
	{
		public SetIsQuantumMessage(bool isQuantum) => Value = isQuantum;

		public override void OnReceiveRemote() => WorldObject.SetIsQuantum(Value);
	}
}