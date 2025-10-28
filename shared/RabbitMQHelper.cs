using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BiSoyle.Shared;

public class RabbitMQHelper
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQHelper()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "admin",
            Password = "admin123"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void PublishMessage<T>(string queueName, T message)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var body = JsonSerializer.Serialize(message);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: Encoding.UTF8.GetBytes(body));
    }

    public void ConsumeMessage<T>(string queueName, Action<T> handler)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new global::RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var obj = JsonSerializer.Deserialize<T>(message);
            
            if (obj != null)
            {
                handler(obj);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

public class ReceiptMessage
{
    public string IslemKodu { get; set; } = "";
    public List<ReceiptItemMessage> Items { get; set; } = new();
    public double ToplamTutar { get; set; }
    public int KullaniciId { get; set; }
}

public class ReceiptItemMessage
{
    public int UrunId { get; set; }
    public string UrunAdi { get; set; } = "";
    public int Miktar { get; set; }
    public double BirimFiyat { get; set; }
    public string OlcuBirimi { get; set; } = "";
}





