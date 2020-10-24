﻿using QSB.Events;
using QSB.Messaging;
using QSB.Utility;
using QSB.WorldSync;

namespace QSB.ConversationSync
{
    public class ConversationEvent : QSBEvent<ConversationMessage>
    {
        public override EventType Type => EventType.Conversation;

        public override void SetupListener() => GlobalMessenger<uint, string, ConversationType>.AddListener(EventNames.QSBConversation, Handler);

        public override void CloseListener() => GlobalMessenger<uint, string, ConversationType>.RemoveListener(EventNames.QSBConversation, Handler);

        private void Handler(uint id, string message, ConversationType type) => SendEvent(CreateMessage(id, message, type));

        private ConversationMessage CreateMessage(uint id, string message, ConversationType type) => new ConversationMessage
        {
            AboutId = LocalPlayerId,
            ObjectId = (int)id,
            Type = type,
            Message = message
        };

        public override void OnReceiveRemote(ConversationMessage message)
        {
            switch (message.Type)
            {
                case ConversationType.Character:
                    var translated = TextTranslation.Translate(message.Message).Trim();
                    ConversationManager.Instance.DisplayCharacterConversationBox(message.ObjectId, translated);
                    break;
                case ConversationType.Player:
                    ConversationManager.Instance.DisplayPlayerConversationBox((uint)message.ObjectId, message.Message);
                    break;
                case ConversationType.EndCharacter:
                    if (message.ObjectId == -1)
                    {
                        break;
                    }
                    UnityEngine.Object.Destroy(ConversationManager.Instance.BoxMappings[WorldRegistry.OldDialogueTrees[message.ObjectId]]);
                    break;
                case ConversationType.EndPlayer:
                    UnityEngine.Object.Destroy(PlayerRegistry.GetPlayer((uint)message.ObjectId).CurrentDialogueBox);
                    break;
            }
        }
    }
}
