﻿using OWML.Utils;
using QSB.Events;
using QSB.QuantumSync.WorldObjects;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.QuantumSync.Events
{
	public class QuantumShuffleEvent : QSBEvent<QuantumShuffleMessage>
	{
		public override QSB.Events.EventType Type => QSB.Events.EventType.QuantumShuffle;

		public override void SetupListener() => GlobalMessenger<int, int[]>.AddListener(EventNames.QSBQuantumShuffle, Handler);
		public override void CloseListener() => GlobalMessenger<int, int[]>.RemoveListener(EventNames.QSBQuantumShuffle, Handler);

		private void Handler(int objid, int[] indexArray) => SendEvent(CreateMessage(objid, indexArray));

		private QuantumShuffleMessage CreateMessage(int objid, int[] indexArray) => new QuantumShuffleMessage
		{
			AboutId = LocalPlayerId,
			ObjectId = objid,
			IndexArray = indexArray
		};

		public override void OnReceiveRemote(bool server, QuantumShuffleMessage message)
		{
			if (!QSBCore.HasWokenUp)
			{
				return;
			}
			var obj = QSBWorldSync.GetWorldObject<QSBQuantumShuffleObject>(message.ObjectId).AttachedObject;
			var shuffledObjects = obj.GetValue<Transform[]>("_shuffledObjects");
			var localPositions = obj.GetValue<Vector3[]>("_localPositions");
			for (var i = 0; i < shuffledObjects.Length; i++)
			{
				shuffledObjects[i].localPosition = localPositions[message.IndexArray[i]];
			}
		}
	}
}
