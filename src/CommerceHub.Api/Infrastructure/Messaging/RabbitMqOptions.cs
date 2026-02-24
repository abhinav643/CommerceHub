namespace CommerceHub.Api.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
}