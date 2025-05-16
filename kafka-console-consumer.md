````
.\bin\windows\kafka-console-consumer.bat --topic chat --bootstrap-server localhost:9094
```

````
.\bin\windows\kafka-console-consumer.bat --topic chat --bootstrap-server localhost:9094 --from-beginning
```

````
.\bin\windows\kafka-console-consumer.bat --topic chat --bootstrap-server localhost:9094 --from-beginning --property print.partition=true --property print.offset=true
```


```
.\bin\windows\kafka-console-producer.bat --topic chat --bootstrap-server localhost:9094
>Mensagem teste grupo de consumidores 
>Mensagem teste grupo de consumidores 2
```
