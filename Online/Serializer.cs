﻿using MoreSlugcats;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;

namespace RainMeadow
{
    public class Serializer
    {
        public readonly byte[] buffer;
        public readonly long capacity;
        private long margin;
        public long Position => stream.Position;

        public bool isWriting { get; private set; }
        public bool isReading { get; private set; }

        MemoryStream stream;
        BinaryWriter writer;
        BinaryReader reader;
        private Dictionary<Type, PlayerEvent.EventTypeId> eventTypeIdsByType;
        private int eventCount;
        private long eventHeader;
        private OnlinePlayer currPlayer;
        private int stateCount;
        private long stateHeader;

        public Serializer(long bufferCapacity) 
        {
            this.capacity = bufferCapacity;
            margin = (long)(bufferCapacity * 0.25f);
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);
        }

        internal void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isWriting = true;
            stream.Seek(0, SeekOrigin.Begin);
        }

        internal bool CanFit(PlayerEvent playerEvent)
        {
            return playerEvent.EstimatedSize + margin < capacity;
        }

        internal bool CanFit(ResourceState resourceState)
        {
            return resourceState.EstimatedSize + margin < capacity;
        }

        internal void BeginWriteEvents()
        {
            eventCount = 0;
            eventHeader = stream.Position;
            writer.Write(eventCount);
        }

        internal void WriteEvent(PlayerEvent playerEvent)
        {
            eventCount++;
            writer.Write((byte)playerEvent.eventType);
            playerEvent.CustomSerialize(this);
        }

        internal void EndWriteEvents()
        {
            var temp = stream.Position;
            stream.Position = eventHeader;
            writer.Write(eventCount);
            stream.Position = temp;
        }

        internal void BeginWriteStates()
        {
            stateCount = 0;
            stateHeader = stream.Position;
            writer.Write(stateCount);
        }

        internal void WriteState(ResourceState resourceState)
        {
            stateCount++;
            resourceState.CustomSerialize(this);
        }

        internal void EndWriteStates()
        {
            var temp = stream.Position;
            stream.Position = stateHeader;
            writer.Write(stateCount);
            stream.Position = temp;
        }

        internal void EndWrite()
        {
            currPlayer = null;
            if (!isWriting) throw new InvalidOperationException("not writing");
            isWriting = false;
            writer.Flush();
        }

        internal void Free()
        {
            // unused
        }

        internal void Serialize(ref ulong uLong)
        {
            if (isWriting) writer.Write(uLong);
            if (isReading) uLong = reader.ReadUInt64();
        }

        internal void Serialize(ref OnlinePlayer player)
        {
            if (isWriting)
            {
                writer.Write((ulong)player.id);
            }
            if (isReading)
            {
                player = OnlineManager.PlayerFromId(new CSteamID(reader.ReadUInt64()));
            }
        }

        internal void Serialize(ref OnlineResource onlineResource)
        {
            if (isWriting)
            {
                // todo switch to bytes?
                writer.Write(onlineResource.Identifier());
            }
            if (isReading)
            {
                string r = reader.ReadString();
                onlineResource = OnlineManager.ResourceFromIdentifier(r);
            }
        }

        internal void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            isReading = true;
            stream.Seek(0, SeekOrigin.Begin);
        }

        internal void EndRead()
        {
            currPlayer = null;
            isReading = false;
        }

        internal void WriteHeaders()
        {
            writer.Write(currPlayer.lastIncomingEvent);
        }

        internal void ReadHeaders()
        {
            ulong lastAck = reader.ReadUInt64();
            currPlayer.SetAck(lastAck);
        }

        internal int BeginReadEvents()
        {
            return reader.ReadInt32();
        }

        internal PlayerEvent ReadEvent()
        {
            PlayerEvent e = null;
            switch ((PlayerEvent.EventTypeId)reader.ReadByte())
            {
                case PlayerEvent.EventTypeId.None:
                    break;
                case PlayerEvent.EventTypeId.ResourceRequest:
                    e = new ResourceRequest(null);
                    break;
                case PlayerEvent.EventTypeId.ReleaseRequest:
                    e = new ReleaseRequest(null);
                    break;
                case PlayerEvent.EventTypeId.TransferRequest:
                    e = new TransferRequest(null, null);
                    break;
                case PlayerEvent.EventTypeId.ReleaseResultReleased:
                    e = new ReleaseResult.Released(null);
                    break;
                case PlayerEvent.EventTypeId.ReleaseResultUnsubscribed:
                    e = new ReleaseResult.Unsubscribed(null);
                    break;
                case PlayerEvent.EventTypeId.ReleaseResultError:
                    e = new ReleaseResult.Error(null);
                    break;
                case PlayerEvent.EventTypeId.RequestResultLeased:
                    e = new RequestResult.Leased(null);
                    break;
                case PlayerEvent.EventTypeId.RequestResultSubscribed:
                    e = new RequestResult.Subscribed(null);
                    break;
                case PlayerEvent.EventTypeId.RequestResultError:
                    e = new RequestResult.Error(null);
                    break;
                case PlayerEvent.EventTypeId.TransferResultError:
                    e = new TransferResult.Error(null);
                    break;
                case PlayerEvent.EventTypeId.TransferResultOk:
                    e = new TransferResult.Ok(null);
                    break;
            }

            e.from = currPlayer;
            e.to = OnlineManager.mePlayer;
            e.CustomSerialize(this);
            return e;
        }

        internal int BeginReadStates()
        {
            return reader.ReadInt32();
        }

        internal ResourceState ReadState()
        {
            throw new NotImplementedException();
        }

        internal void Serialize(ref List<OnlinePlayer> players)
        {
            throw new NotImplementedException();
        }
    }
}