using System.Net.WebSockets;
// ???????????????????????? WebSocket ???
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectDuel.Server.Options;
using ProjectDuel.Server.Services;
using ProjectDuel.Shared.Protocol;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5057");

builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection(ServerOptions.SectionName));
builder.Services.Configure<ConfigOptions>(builder.Configuration.GetSection(ConfigOptions.SectionName));
builder.Services.AddSingleton<AuthoritativeConfigService>();
builder.Services.AddSingleton<RoomService>();

var app = builder.Build();
var logger = app.Logger;

var configService = app.Services.GetRequiredService<AuthoritativeConfigService>();
configService.Load();
app.UseWebSockets();

app.MapGet("/health", (IOptions<ServerOptions> options, AuthoritativeConfigService config) => Results.Ok(new
{
    name = options.Value.Name,
    utcNow = DateTimeOffset.UtcNow,
    status = "ok",
    skillRuleCount = config.SkillRuleCount,
}));

app.MapGet("/", () => Results.Text("ProjectDuel authoritative server is running."));

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
    RoomService roomService = context.RequestServices.GetRequiredService<RoomService>();
    var session = roomService.RegisterSocket(socket);

    try
    {
        await roomService.SendConnectedAsync(session);
        await ReceiveLoopAsync(roomService, session.SessionId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "WebSocket session {SessionId} failed.", session.SessionId);
    }
    finally
    {
        await roomService.UnregisterSocketAsync(session.SessionId);
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        socket.Dispose();
    }
});

app.Run();

static async Task ReceiveLoopAsync(RoomService roomService, string sessionId)
{
    ConnectedSessionAccessor accessor = roomService.GetAccessor(sessionId);
    byte[] buffer = new byte[16 * 1024];
    JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    while (accessor.Socket.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result = await accessor.Socket.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
            break;

        int count = result.Count;
        while (!result.EndOfMessage)
        {
            if (count >= buffer.Length)
                throw new InvalidOperationException("Incoming websocket message too large.");
            result = await accessor.Socket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), CancellationToken.None);
            count += result.Count;
        }

        string json = Encoding.UTF8.GetString(buffer, 0, count);
        ClientEnvelope? envelope = JsonSerializer.Deserialize<ClientEnvelope>(json, jsonOptions);
        if (envelope == null || string.IsNullOrWhiteSpace(envelope.Type))
            continue;

        await roomService.HandleClientMessageAsync(sessionId, envelope);
    }
}
