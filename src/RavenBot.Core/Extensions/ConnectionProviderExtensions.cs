using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RavenBot.Core.Net.WebSocket;

namespace RavenBot.Core.Extensions
{
    public static class ConnectionProviderExtensions
    {
        public static Task<bool> BroadcastAsync<T>(this IConnectionProvider connectionProvider, T update)
        {
            return connectionProvider.ForAllConnectionsAsync(x => x.SendAsync(update));
        }

        private static async Task<bool> ForAllConnectionsAsync(this IConnectionProvider connectionProvider, Func<IConnection, Task> action)
        {
            var connection = connectionProvider.GetAll();
            if (connection == null) return false;
            await connection.ForEachAsync(action);
            return true;
        }
    }
}
