using Aelena.FileApi.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<FileServiceImpl>();
app.MapGet("/", () => "FileApi gRPC service. Use a gRPC client to connect.");

app.Run();

public partial class Program;
