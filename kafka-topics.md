### Criando um tópico
```
.\bin\windows\kafka-topics.bat --create --topic cursos --bootstrap-server localhost:9094
```

### Listando tópicos
```
.\bin\windows\kafka-topics.bat --list --bootstrap-server localhost:9094
```

```
.\bin\windows\kafka-topics.bat --list --bootstrap-server localhost:9095
```

```
.\bin\windows\kafka-topics.bat --list --bootstrap-server localhost:9094 localhost:9095
```

### Excluindo tópicos
```
.\bin\windows\kafka-topics.bat --delete --topic cursos --bootstrap-server localhost:9094
```

### Criando um tópico com partição
```
.\bin\windows\kafka-topics.bat --create --topic chat --bootstrap-server localhost:9094 --partitions 2 --replication-factor 2
```

### Alterando quantidade de partições de um tópico
```
.\bin\windows\kafka-topics.bat --alter --topic chat --bootstrap-server localhost:9094 --partitions 3
```

### Obtendo informações de um tópico
```
.\bin\windows\kafka-topics.bat --describe --topic chat --bootstrap-server localhost:9094
```
