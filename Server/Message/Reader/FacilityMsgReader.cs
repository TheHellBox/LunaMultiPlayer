﻿using LunaCommon.Message.Data.Facility;
using LunaCommon.Message.Interface;
using LunaCommon.Message.Server;
using LunaCommon.Message.Types;
using Server.Client;
using Server.Log;
using Server.Message.Reader.Base;
using Server.Server;
using System;

namespace Server.Message.Reader
{
    public class FacilityMsgReader : ReaderBase
    {
        public override void HandleMessage(ClientStructure client, IMessageData messageData)
        {
            var baseMsg = (FacilityBaseMsgData)messageData;
            switch (baseMsg.FacilityMessageType)
            {
                case FacilityMessageType.Upgrade:
                    var upgradeMsg = (FacilityUpgradeMsgData)messageData;
                    LunaLog.Normal($"{client.PlayerName} UPGRADED facility {upgradeMsg.ObjectId} to level: {upgradeMsg.Level}");
                    break;
                case FacilityMessageType.Repair:
                    LunaLog.Normal($"{client.PlayerName} REPAIRED facility {baseMsg.ObjectId}");
                    break;
                case FacilityMessageType.Collapse:
                    LunaLog.Normal($"{client.PlayerName} DESTROYED facility {baseMsg.ObjectId}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //We don't do anything on the server side with this messages so just relay them.
            MessageQueuer.RelayMessage<FacilitySrvMsg>(client, messageData);
        }
    }
}