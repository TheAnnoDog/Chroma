using System.Threading;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Models;

namespace Chroma
{
    class LightInfo
    {
        public static StreamingHueClient client;
        public static EntertainmentLayer layer;
        public static CancellationToken token;

        public static void setInfo(StreamingHueClient input, EntertainmentLayer input1, CancellationToken input2)
        {
            client = input;
            layer = input1;
            token = input2;
        }

        public static void disconnect()
        {
            client = null;
            layer = null;
        }
    }
}
