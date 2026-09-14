using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FableAstra;

public static class ApiSelfTest
{
    public static async Task Run()
    {
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;
        var requests=new List<string>();
        var serve=Task.Run(async ()=>
        {
            for(int i=0;i<3;i++)
            {
                using var connection=await listener.AcceptTcpClientAsync();
                await using var stream=connection.GetStream();
                var header=new List<byte>();var one=new byte[1];
                while(await stream.ReadAsync(one)==1)
                {
                    header.Add(one[0]);int n=header.Count;
                    if(n>=4&&header[n-4]==13&&header[n-3]==10&&header[n-2]==13&&header[n-1]==10)break;
                }
                var headers=Encoding.ASCII.GetString(header.ToArray());
                int length=0;
                foreach(var row in headers.Split("\r\n")) if(row.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))length=int.Parse(row[15..].Trim());
                var body=new byte[length];await stream.ReadExactlyAsync(body);
                requests.Add(Encoding.UTF8.GetString(body));
                string json=i==2?"{}":"{\"choices\":[{\"message\":{\"content\":\"A cozy little corner sounds lovely.\"}}]}";
                string response="HTTP/1.1 "+(i==2?"401 Unauthorized":"200 OK")+"\r\nContent-Type: application/json\r\nConnection: close\r\nContent-Length: "+Encoding.UTF8.GetByteCount(json)+"\r\n\r\n"+json;
                await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
            }
        });
        using var dialogue=new Dialogue();
        var config=new Settings {LiveAI=true,Fable=new(){Endpoint=$"http://127.0.0.1:{port}/v1",Model="fable-test",KeyVariable=""},Astra=new(){Endpoint=$"http://127.0.0.1:{port}/v1",Model="astra-test",KeyVariable=""}};
        var fable=await dialogue.NextLive(0,config,CancellationToken.None);
        var astra=await dialogue.NextLive(1,config,CancellationToken.None);
        if(fable.Character!=0||astra.Character!=1||dialogue.History.Count!=2)throw new Exception("Live AI turn-taking failed");
        bool failed=false;try{await dialogue.NextLive(0,config,CancellationToken.None);}catch(InvalidOperationException ex){failed=ex.Message.Contains("401");}
        if(!failed)throw new Exception("HTTP failure not surfaced");
        await serve.WaitAsync(TimeSpan.FromSeconds(5));
        using var first=JsonDocument.Parse(requests[0]);using var second=JsonDocument.Parse(requests[1]);
        if(first.RootElement.GetProperty("model").GetString()!="fable-test"||second.RootElement.GetProperty("model").GetString()!="astra-test")throw new Exception("Characters must use their own models");
        if(!second.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!.Contains(fable.Text))throw new Exception("Partner's reply missing from context");
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        bool cancelled=false;try{await dialogue.NextLive(0,config,cancel.Token);}catch(OperationCanceledException){cancelled=true;}
        if(!cancelled)throw new Exception("AI cancellation failed");
    }
}
