﻿using System.Collections.Generic;
using System.Linq;
using QSB.Events;
using QSB.Utility;
using QuantumUNET;
using QuantumUNET.Components;

namespace QSB.AuthoritySync
{
	public static class AuthorityManager
	{
		#region host only

		/// whoever is first gets authority
		private static readonly Dictionary<QNetworkIdentity, List<uint>> _authQueue = new();

		public static void RegisterAuthQueue(this QNetworkIdentity identity) => _authQueue.Add(identity, new List<uint>());
		public static void UnregisterAuthQueue(this QNetworkIdentity identity) => _authQueue.Remove(identity);

		public static void UpdateAuthQueue(this QNetworkIdentity identity, uint id, bool queue)
		{
			var authQueue = _authQueue[identity];

			var oldAuthority = authQueue.Contains(id);
			if (queue == oldAuthority)
			{
				return;
			}

			if (queue)
			{
				authQueue.Add(id);
			}
			else
			{
				authQueue.Remove(id);
			}

			var newOwner = authQueue.Count != 0 ? authQueue[0] : uint.MaxValue;
			SetAuthority(identity, newOwner);
		}

		/// transfer authority to a different client
		public static void OnDisconnect(uint id)
		{
			foreach (var identity in _authQueue.Keys)
			{
				identity.UpdateAuthQueue(id, false);
			}
		}

		public static void SetAuthority(this QNetworkIdentity identity, uint id)
		{
			var oldConn = identity.ClientAuthorityOwner;
			var newConn = id != uint.MaxValue
				? QNetworkServer.connections.First(x => x.GetPlayerId() == id)
				: null;

			if (oldConn == newConn)
			{
				return;
			}

			if (oldConn != null)
			{
				identity.RemoveClientAuthority(oldConn);
			}

			if (newConn != null)
			{
				identity.AssignClientAuthority(newConn);
			}

			// DebugLog.DebugWrite($"{identity.NetId}:{identity.gameObject.name} - "
				// + $"set authority to {id}");
		}

		#endregion

		#region any client

		public static void FireAuthQueue(this QNetworkIdentity identity, bool queue) =>
			QSBEventManager.FireEvent(EventNames.QSBAuthorityQueue, identity, queue);

		#endregion
	}
}
