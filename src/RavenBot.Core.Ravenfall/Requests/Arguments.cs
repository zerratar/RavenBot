namespace RavenBot.Core.Ravenfall.Requests
{
    public class Arguments
    {
        public Arguments(params object[] values)
        {
            this.Values = values;
        }
        public object[] Values { get; }
    }
}