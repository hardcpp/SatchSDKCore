using SSC.Config;
using Newtonsoft.Json;

namespace DemoApp.Config;

internal class Database() : JsonConfig<Database>("Config/")
{
    [JsonProperty] internal string  Driver          = "postgresql";
    [JsonProperty] internal string  Hostname        = "localhost";
    [JsonProperty] internal uint    Port            = 5432;
    [JsonProperty] internal string  Username        = "myuser";
    [JsonProperty] internal string  Password        = "mypassword";
    [JsonProperty] internal string  DatabaseName    = "mydatabase";
    [JsonProperty] internal uint    PoolSize        = 5;
}
