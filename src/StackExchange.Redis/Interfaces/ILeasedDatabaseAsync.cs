using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Redis.Interfaces
{
    /// <summary>
    /// Describes functionality for blocking operations.
    /// </summary>
    public interface ILeasedDatabaseAsync : IDatabaseAsync
    {
        /// <summary>
        /// Removes and returns the first element of the list stored at key(s).
        /// </summary>
        /// <param name="keys">One or more list keys.</param>
        /// <param name="timeout">The time to wait for items in the list(s).</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <returns>KeyValuePair for each list that returned an item.</returns>
        /// <remarks>https://redis.io/commands/blpop</remarks>
        Task<KeyValuePair<string, string>[]> ListLeftPopAsync(RedisKey[] keys, TimeSpan timeout, CommandFlags flags = CommandFlags.None);
    }
}
