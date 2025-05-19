using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

var schemaConfig = new SchemaRegistryConfig
{
    Url = "http://localhost:8081"
};

var schemaRegistry = new CachedSchemaRegistryClient(schemaConfig);

var config = new ProducerConfig { BootstrapServers = "localhost:9092" };

using var producer = new ProducerBuilder<string, desenvolvedor.io.Curso>(config)
    .SetValueSerializer(new AvroSerializer<desenvolvedor.io.Curso>(schemaRegistry))
    .Build();

var message = new Message<string, desenvolvedor.io.Curso>
{
    Key = Guid.NewGuid().ToString(),
    Value = new desenvolvedor.io.Curso
    {
        Id = Guid.NewGuid().ToString(),
        Descricao = "Curso de Apache Kafka"
    }
};

var result = await producer.ProduceAsync("cursos", message);

Console.WriteLine($"{result.Offset}");
