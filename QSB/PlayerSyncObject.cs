﻿using System;
using UnityEngine.Networking;

namespace QSB
{
    public abstract class PlayerSyncObject : NetworkBehaviour
    {
        public NetworkInstanceId NetId => GetComponent<NetworkIdentity>()?.netId ?? NetworkInstanceId.Invalid;
        public bool IsLocal => hasAuthority;
        [Obsolete("Use Player.PlayerId instead")]
        public NetworkInstanceId PlayerId => this.GetPlayerOfObject();
        public PlayerInfo Player => PlayerRegistry.GetPlayer(PlayerId);
    }
}
