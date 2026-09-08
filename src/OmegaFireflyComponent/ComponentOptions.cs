namespace OmegaFireflyComponent;

public class ComponentOptions
{
    public const string SectionName = "Component";

    public string RabbitMqHost { get; set; } = "127.0.0.1";
    public int RabbitMqPort { get; set; } = 5672;
    public string RabbitMqUsername { get; set; } = "admin";
    public string RabbitMqPassword { get; set; } = "admin";

    public string InputExchange { get; set; } = "omega-solider-input";
    public string InputQueue { get; set; } = "omega-solider-input-queue";
    public string InputRoutingKey { get; set; } = "/";

    public string OutputExchange { get; set; } = "firefly-expert-output";
    public string OutputRoutingKey { get; set; } = "/";

    public bool EnableExternalApi { get; set; } = false;
    public string ExternalApiBaseUrl { get; set; } = "http://127.0.0.1:8080";
}
