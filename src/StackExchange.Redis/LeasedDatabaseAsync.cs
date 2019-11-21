using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis.Interfaces;

namespace StackExchange.Redis
{
    /// <summary>
    /// 
    /// </summary>
    internal class LeasedDatabaseAsync : RedisDatabase, ILeasedDatabaseAsync, IDatabaseAsync
    {
        internal LeasedDatabaseAsync(ConnectionMultiplexer multiplexer, int db, object asyncState)
            : base(multiplexer, db, asyncState)
        { }

        public Task<KeyValuePair<string, string>[]> ListLeftPopAsync(RedisKey[] keys, TimeSpan timeout, CommandFlags flags = CommandFlags.None)
        {
            if (keys == null) throw new NullReferenceException(nameof(keys));
            if (keys.Length == 0) throw new ArgumentException("Parameter 'keys' must contain at least one RedisKey.");

            ThrowIfInvalidTimeout(timeout);

            var values = new RedisValue[keys.Length + 1];
            var offset = 0;

            foreach (var key in keys)
            {
                values[offset++] = key.AsRedisValue();
            }

            values[offset] = timeout.TotalSeconds;

            // TODO: mark the message blocking so it knows to get a dedicated connection to run the operation on.

            var msg = Message.Create(Database, flags, RedisCommand.BLPOP, values);
            return ExecuteAsync(msg, ResultProcessor.StringPairInterleaved);
        }

        private void ThrowIfInvalidTimeout(TimeSpan timeout)
        {
            if (timeout.TotalSeconds == 0) throw new ArgumentException("Invalid timeout, the total seconds must be greater than 0.");
        }
    }
}
