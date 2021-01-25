﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nami.EventListeners.Attributes;

namespace Nami.EventListeners
{
    internal static partial class Listeners
    {
        public static IEnumerable<ListenerMethod> ListenerMethods { get; private set; } = Enumerable.Empty<ListenerMethod>();

        public static void FindAndRegister(NamiBot shard)
        {
            ListenerMethods =
                from t in Assembly.GetExecutingAssembly().GetTypes()
                from m in t.GetMethods()
                let a = m.GetCustomAttribute(typeof(AsyncEventListenerAttribute), inherit: true)
                where a is { }
                select new ListenerMethod(m, (AsyncEventListenerAttribute)a);

            foreach (ListenerMethod lm in ListenerMethods)
                lm.Attribute.Register(shard, lm.Method);
        }
    }


    internal sealed class ListenerMethod
    {
        public MethodInfo Method { get; }
        public AsyncEventListenerAttribute Attribute { get; }

        public ListenerMethod(MethodInfo mi, AsyncEventListenerAttribute attr)
        {
            this.Method = mi;
            this.Attribute = attr;
        }
    }
}