using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

var schemaConfig = new SchemaRegistryConfig
{
    Url = "http://localhost:8081"
};

var schemaRegistry = new CachedSchemaRegistryClient(schemaConfig);

var config = new ConsumerConfig
{
    GroupId = "devio",
    BootstrapServers = "localhost:9092"
};

using var consumer = new ConsumerBuilder<string, desenvolvedor.io.Curso>(config)
    .SetValueDeserializer(new AvroDeserializer<desenvolvedor.io.Curso>(schemaRegistry).AsSyncOverAsync())
    .Build();

consumer.Subscribe("cursos");

while (true)
{
    var result = consumer.Consume();

    Console.WriteLine($"Mensagem: {result.Message.Value.Descricao}");
}
