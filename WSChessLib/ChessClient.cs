using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using ChessLib.ChessServer;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChessLib
{
    public class ChessClient : ChessServer.ChessClientBase
    {
        public string CreateDb(string serviceUrl)
        {
#if false
            HttpClient client = new HttpClient();
//            client.Headers["Content-type"] = "application/json";

            string uri = string.Format(@"{0}/ChessService.svc/CreateDb", serviceUrl);

            HttpResponseMessage response = await client.GetAsync(uri);
            byte[] down = await response.Content.ReadAsByteArrayAsync();
            MemoryStream stream = new MemoryStream(down);

            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(string));
            string result = (string)obj.ReadObject(stream);
            return result;
#else
            return string.Empty;
#endif
        }

        protected override async void MakeNetworkCall<T>(string uri, NetworkResult<T> func)
        {
            ChessLib.ChessServer.ReturnValue<T> result = await MakeNetworkCall<T>(uri);

            func(result);
        }

        protected async Task<ReturnValue<T>> MakeNetworkCall<T>(string uri)
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(uri);

            byte[] data = await response.Content.ReadAsByteArrayAsync();

            Stream stream = new MemoryStream(data);

            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(ChessLib.ChessServer.ReturnValue<T>));
            ChessLib.ChessServer.ReturnValue<T> result = (ChessLib.ChessServer.ReturnValue<T>)obj.ReadObject(stream);

            return result;
        }

        protected override async void MakeNetworkCall<R1, R2>(string uri, NetworkResult<R1, R2> func)
        {
            ChessLib.ChessServer.ReturnValue<R1, R2> result = await MakeNetworkCall<R1, R2>(uri);
            func(result);
        }

        protected async Task<ReturnValue<R1, R2>> MakeNetworkCall<R1, R2>(string uri)
        {
            HttpClient client = new HttpClient();

            //            System.Diagnostics.Trace.WriteLine(string.Format("MakeNetworkCall {0}", uri));

            HttpResponseMessage response = await client.GetAsync(uri);

            byte[] data = await response.Content.ReadAsByteArrayAsync();

            //            System.Diagnostics.Trace.WriteLine(string.Format("downloaded {0} bytes", data.Length));

            Stream stream = new MemoryStream(data);

            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(ChessLib.ChessServer.ReturnValue<R1, R2>));
            ChessLib.ChessServer.ReturnValue<R1, R2> result = (ChessLib.ChessServer.ReturnValue<R1, R2>)obj.ReadObject(stream);

            return result;
        }

        protected override async void MakeNetworkCall<T, P>(string uri, NetworkResult<T> func, P param)
        {
            ChessLib.ChessServer.ReturnValue<T> result = await MakeNetworkCall<T, P>(uri, param);
            func(result);
        }

        protected async Task<ReturnValue<T>> MakeNetworkCall<T, P>(string uri, P param)
        {
            HttpClient client = new HttpClient();

            MemoryStream upStream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(P));
            serializer.WriteObject(upStream, param);

            //            byte[] upData = upStream.ToArray();

            HttpContent content = new StreamContent(upStream);
            HttpResponseMessage response = await client.PostAsync(uri, content);

            //            System.Diagnostics.Trace.WriteLine(string.Format("MakeNetworkCall {0} up {1}", uri, upData.Length));

            byte[] data = await response.Content.ReadAsByteArrayAsync();

            //            System.Diagnostics.Trace.WriteLine(string.Format("downloaded {0} bytes", data.Length));

            Stream stream = new MemoryStream(data);

            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(ChessLib.ChessServer.ReturnValue<T>));
            ChessLib.ChessServer.ReturnValue<T> result = (ChessLib.ChessServer.ReturnValue<T>)obj.ReadObject(stream);

            return result;
        }

        protected override async void MakeNetworkCall<R1, R2, P1>(string uri, NetworkResult<R1, R2> func, P1 param1)
        {
            ChessLib.ChessServer.ReturnValue<R1, R2> result = await MakeNetworkCall<R1, R2, P1>(uri, param1);
            func(result);
        }

        protected async Task<ReturnValue<R1, R2>> MakeNetworkCall<R1, R2, P1>(string uri, P1 param1)
        {
            HttpClient client = new HttpClient();

            MemoryStream upStream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(P1));
            serializer.WriteObject(upStream, param1);

            //            byte[] upData = upStream.ToArray();

            HttpContent content = new StreamContent(upStream);
            HttpResponseMessage response = await client.PostAsync(uri, content);


            //            System.Diagnostics.Trace.WriteLine(string.Format("MakeNetworkCall {0} up {1}", uri, upData.Length));

            byte[] data = await response.Content.ReadAsByteArrayAsync();

            //            System.Diagnostics.Trace.WriteLine(string.Format("downloaded {0} bytes", data.Length));

            Stream stream = new MemoryStream(data);

            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(ChessLib.ChessServer.ReturnValue<R1, R2>));
            ChessLib.ChessServer.ReturnValue<R1, R2> result = (ChessLib.ChessServer.ReturnValue<R1, R2>)obj.ReadObject(stream);

            return result;
        }
    }

    public class WSChessClient : ChessClient
    {
        public async Task<ReturnValue<int, string>> CreatePlayer(string name, string password, string email)
        {
            string uri = GetUri_CreatePlayer(name, password, email);
            ReturnValue<int, string> result = await MakeNetworkCall<int, string>(uri);
            return result;
        }

        public async Task<ReturnValue<GameToken>> SeekGame()
        {
            string uri = GetUri_SeekGame();
            ReturnValue<GameToken> result = await MakeNetworkCall<GameToken, LoginToken>(uri, LoginToken);
            return result;
        }

        public async Task<ReturnValue<List<ChessLib.ChessServer.GameToken>>> GetGames()
        {
            string uri = GetUri_GetGames();
            ReturnValue<List<ChessLib.ChessServer.GameToken>> result = await MakeNetworkCall<List<ChessLib.ChessServer.GameToken>, LoginToken>(uri, LoginToken);
            return result;
        }

        public async Task<ReturnValue<ChessGameData>> GetGame(int gameId)
        {
            string uri = GetUri_GetGame();
            ReturnValue<ChessGameData> result = await MakeNetworkCall<ChessGameData, GetBoardParameters>(uri, new GetBoardParameters { LoginToken = LoginToken, GameId = gameId });
            return result;
        }

        public async Task<ReturnValue<int>> MakeMove(int gameId, ChessGameData chessGameData)
        {
            string uri = GetUri_MakeMove();
            ReturnValue<int> result = await MakeNetworkCall<int, MakeMoveParameters>(uri, new MakeMoveParameters { LoginToken = LoginToken, GameId = gameId, GameData = chessGameData });
            return result;
        }
    }
}
