namespace Learning.Infrastructure.Messaging;
public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}