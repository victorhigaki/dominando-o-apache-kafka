using Confluent.Kafka;

var config = new ConsumerConfig
{
    GroupId = "devio",
    BootstrapServers = "localhost:9092"
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();

consumer.Subscribe("topico-teste");

while (true)
{
    var result = consumer.Consume();

    Console.WriteLine($"Mensagem: {result.Message.Key}-{result.Message.Value}");
}
