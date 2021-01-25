﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nami.Common;

namespace Nami.Extensions
{
    internal static class EnumerableExtensions
    {
        public static T MaxBy<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector) where TResult : IComparable<TResult>
            => source.Aggregate((e1, e2) => selector(e1).CompareTo(selector(e2)) > 0 ? e1 : e2);

        public static T MinBy<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector) where TResult : IComparable<TResult>
            => source.Aggregate((e1, e2) => selector(e1).CompareTo(selector(e2)) > 0 ? e2 : e1);

        public static string JoinWith<T>(this IEnumerable<T> source, string separator = "\n")
            => string.Join(separator, source.Select(e => e?.ToString() ?? ""));

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
            => source.Shuffle(new SecureRandom());

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, SecureRandom rng)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (rng is null)
                throw new ArgumentNullException(nameof(rng));

            return source.ShuffleIterator(rng);
        }


        private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, SecureRandom rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++) {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
}
