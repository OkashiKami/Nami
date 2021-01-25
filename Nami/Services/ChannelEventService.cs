﻿using System.Collections.Concurrent;
using Nami.Common;

namespace Nami.Services
{
    public sealed class ChannelEventService : INamiService
    {
        public bool IsDisabled => false;

        private readonly ConcurrentDictionary<ulong, IChannelEvent?> events;


        public ChannelEventService()
        {
            this.events = new ConcurrentDictionary<ulong, IChannelEvent?>();
        }


        public IChannelEvent? GetEventInChannel(ulong cid)
            => this.events.TryGetValue(cid, out IChannelEvent? e) && e is { } ? e : null;

        public T? GetEventInChannel<T>(ulong cid) where T : class, IChannelEvent
            => this.GetEventInChannel(cid) as T;

        public bool IsEventRunningInChannel(ulong cid)
            => this.GetEventInChannel(cid) is { };

        public bool IsEventRunningInChannel(ulong cid, out IChannelEvent? @event)
        {
            @event = null;
            IChannelEvent? chnEvent = this.GetEventInChannel(cid);
            if (chnEvent is null)
                return false;
            @event = chnEvent;
            return true;
        }

        public bool IsEventRunningInChannel<T>(ulong cid, out T? @event) where T : class, IChannelEvent
        {
            @event = null;
            IChannelEvent? chnEvent = this.GetEventInChannel<T>(cid);
            if (chnEvent is null or not T)
                return false;
            @event = chnEvent as T;
            return true;
        }

        public void RegisterEventInChannel(IChannelEvent cevent, ulong cid)
        {
            if (this.IsEventRunningInChannel(cid))
                throw new System.InvalidOperationException("Another event is running in channel!");
            this.events.AddOrUpdate(cid, cevent, (c, e) => cevent);
        }

        public void UnregisterEventInChannel(ulong cid)
        {
            if (!this.events.TryRemove(cid, out _))
                this.events[cid] = null;
        }
    }
}
