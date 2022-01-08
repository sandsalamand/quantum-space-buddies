﻿using OWML.Common;
using QSB.Messaging;
using QSB.Player;
using QSB.Utility;

namespace QSB.ClientServerStateSync.Messages
{
	internal class ClientStateMessage : QSBEnumMessage<ClientState>
	{
		public ClientStateMessage(ClientState state) => Value = state;

		public override void OnReceiveLocal()
			=> ClientStateManager.Instance.ChangeClientState(Value);

		public override void OnReceiveRemote()
		{
			if (From == uint.MaxValue)
			{
				DebugLog.ToConsole($"Error - ID is uint.MaxValue!", MessageType.Error);
				return;
			}

			var player = QSBPlayerManager.GetPlayer(From);
			player.State = Value;
		}
	}
}