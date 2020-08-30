# Kafka Workshop Part 3: Kafka Connect with Event Streams

In this exercise you will use **Kafka Connect** to transfer data in real-time between a source and sink with an event stream processor that can perform data transformations.

> **Note**: [Kafka Connect](https://docs.confluent.io/current/connect/index.html) is a framework for connecting Apache Kafka with external systems such as databases. It is an open source component of Kafka.

### Prerequisites

1. Install [Docker Desktop](https://docs.docker.com/desktop/).
   - You will need at least 8 GB of available memory.
2. Open a terminal at the project root and run `docker-compose up --build -d`.
   - To check the running containers run `docker-compose ps`.
   - To bring down the containers run `docker-compose down`.
3. Open a browser to http://localhost:9021/.
   - Verify the cluster is healthy. (This may take a few minutes.)

## Docker Compose

> **Note**: If the **mongo-express** service exits, simply bring the services down and up again. At times volumes may not be properly unmounted, in which case you need to remove them by running: `docker-compose down --volumes`.

1. Inspect the **docker-compose.yml** file in the solution root directory.
   - This is based on the [confluent/cp-all-in-one](https://github.com/confluentinc/cp-all-in-one) Github repository, but it has some significant modifications.
   - Services for **zookeeper**, **broker**, **schema-registry** and **control-center** are unchanged.
   - In the **connect** service the key and value converters are set to use Protobuf.
      ```yaml
      CONNECT_KEY_CONVERTER: io.confluent.connect.protobuf.ProtobufConverter
      CONNECT_VALUE_CONVERTER: io.confluent.connect.protobuf.ProtobufConverter
      ```
   - The **connect** service also has a `build` directive that uses a **Dockerfile** at the solution root.
      ```yaml
      build: ./
      ```
   - The Dockerfile installs the `jdbc` and `mongodb` connectors on the `confluentinc/cp-kafka-connect-base` image.
      ```docker
      FROM confluentinc/cp-kafka-connect-base:5.5.1
      RUN  confluent-hub install --no-prompt confluentinc/kafka-connect-jdbc:5.5.1
      RUN  confluent-hub install --no-prompt mongodb/kafka-connect-mongodb:1.2.0
      ```
   - The following services are added for databases and their admin tools: **postgres**, **pgadmin**, **mongo**, **mongo-express**.
     - Postgres and MongoDB admin tools can be used via a web browser.

   > **Note**: The Postgres docker image used is [debezium/postgres](https://hub.docker.com/r/debezium/postgres), which has the [decoderbufs](https://debezium.io/documentation/reference/1.2/connectors/postgresql.html) output plugin installed.

## Source Connector

1. Open **pgadmin** and connect to **postgres**.
   - Navigate to http://localhost:5050
   - Username: pgadmin4@pgadmin.org
   - Password: admin
   - Click **Add New Server**
      - Name: postgres
      - Connection **host name**: postgres
      - Connection **username**: postgres
      - Connection **password**: mypassword

2. Create `person` table.
   - Select **source-database**.
   - Right-click and select Query Tool.
   - Execute script to create table.
   ```sql
   CREATE TABLE public.person
   (
      person_id serial NOT NULL,
      first_name text NOT NULL,
      last_name text NOT NULL,
      favorite_color text NOT NULL,
      age integer NOT NULL,
      row_version timestamp with time zone NOT NULL,
      CONSTRAINT person_pkey PRIMARY KEY (person_id)
   )
   ```

3. Register **JDBC** source connector.
   - JDBC connector [features](https://docs.confluent.io/current/connect/kafka-connect-jdbc/source-connector/index.html#features) and [deep dive](https://www.confluent.io/blog/kafka-connect-deep-dive-jdbc-source-connector/).
   - JDBC connector [configuration](https://docs.confluent.io/current/connect/kafka-connect-jdbc/source-connector/source_config_options.html).

   - *Option 1*: Upload the connector config `json` file using the **Control Center** user interface.
      - The **register-postgres-source.json** file is located in the **Connectors** folder.
   - *Option 2*: Run the following `curl` command to upload the config `json` file.

    ```bash
    curl -i -X POST -H "Accept:application/json" -H  "Content-Type:application/json" http://localhost:8083/connectors/ -d @Connectors/register-postgres-source.json
    ```

4. Add a row to the Postgres `public.person` database.
   - Open **pgAdmin** and run the following SQL.
   ```sql
   INSERT INTO public.person(
      first_name, last_name, favorite_color, age, row_version)
      VALUES ('Mickey', 'Mouse', 'Green', 10, now());
   ```

## ProtoLibrary and TransferTest

1. Add `person-source.v1.proto` file to **ProtoLibrary**.
   - Open the Control Center, click **Topics**.
   - Select **source-data-person**, Schema, Value.
   - Copy the schema contents.
   - Delete **placeholder.txt** from the **Protos** folder of **ProtoLibrary**.
   - Create **person-source.v1.proto** file in **Protos** folder of **ProtoLibrary**.
   - Paste copied schema into the file.
   - Add `option csharp_namespace = "Protos.Source.v1";` The final version should look like the following.
      ```protobuf
      syntax = "proto3";

      option csharp_namespace = "Protos.Source.v1";

      import "google/protobuf/timestamp.proto";

      message person {
        int32 person_id = 1;
        string first_name = 2;
        string last_name = 3;
        string favorite_color = 4;
        int32 age = 5;
        google.protobuf.Timestamp row_version = 6;
      }
      ```
   - Build the solution.
   - Locate the **PersonSourceV1.cs** file in **TransferTest**, *obj, Debug, netcoreapp3.1*.

2. Add `key-sink.v1.proto` file to **ProtoLibrary**.
   - Create **key-sink.v1.proto** file in **Protos** folder of **ProtoLibrary**.
   - Paste the following.
      ```protobuf
      syntax = "proto3";

      option csharp_namespace = "Protos.Sink.v1";

      message Key {
        int32 person_id = 1;
      }
      ```
   - Build again and you should see **KeySinkV1.cs** appear in *obj, Debug, netcoreapp3.1*.
   > **Note**: The JDBC source connector will not produce a key, but you can create one for the Mongo sink connector. With some more work, you could customize the JDBC connector, but it would be better to switch to a CDC connector that will generate keys.

3. Add `person-sink.v1.proto` file to **ProtoLibrary**.
   - Create **person-sink.v1.proto** file in **Protos** folder of **ProtoLibrary**.
   - Copy the content of **person-source.v1.proto**, then replace `Source` with `Sink` in the namespace, and merge first and last name fields into `name`. The content should resemle the following.
      ```protobuf
      syntax = "proto3";

      option csharp_namespace = "Protos.Sink.v1";

      import "google/protobuf/timestamp.proto";

      message person {
        int32 person_id = 1;
        string name = 2;
        string favorite_color = 3;
        int32 age = 4;
        google.protobuf.Timestamp row_version = 5;
      }
      ```
   - Build again and you should see **PersonSinkV1.cs** appear in *obj, Debug, netcoreapp3.1*.
4. Update the **TransferTest** app to use the generated Protobuf classes.
   - Go through **Program.cs** and uncomment code beneath each `// TODO:` that you see.

5. Run the **TransferTest** app to validate that data is flowing from Postgres to Kafka.
   - Set a breakpoint on the call to `PrintConsumeResult` in `Program.Run_Consumer`.
   - Press F5 to start the debugger.
   - Validate that the message is properly deserialized and displayed in the terminal.

## Sink Connector

1. Open **Mongo Express** and connect to **mongo**.
   - Navigate to http://localhost:8080
   - Username: mongoadmin
   - Password: admin
   - Click **Create Database**
     - Name: sink-database
     - Select sink-database to view
   - Click **Create Collection**
     - Name: person

2. Register **MongoDB** sink connector.
   - MongoDB connector [documentation](https://docs.mongodb.com/kafka-connector/master/kafka-sink-properties/).
   > **Step 1**: Set the `topics` entry in **register-mongo-sink.json** to `source-data-person`, so that data will flow directly to MongoDB from the topic written to by the Postgres connector.
   - *Option 1*: Upload the connector config `json` file using the **Control Center** user interface.
      - The **register-mongo-sink.json** file is located in the **Connectors** folder.
   - *Option 2*: Run the following `curl` command to upload the config `json` file.

    ```bash
    curl -i -X POST -H "Accept:application/json" -H  "Content-Type:application/json" http://localhost:8083/connectors/ -d @Connectors/register-mongo-sink.json
    ```

3. In **Mongo Express** validate that a record was added to the `person` collection.
   - After validating tht the record was created, you can delete the record.

4. Write messages to a separate topic for the MongoDB sink database.
   > **Step 2**: Set the `topics` entry in **register-mongo-sink.json** back to `sink-data-person`, so that data will flow to MongoDB through the **TransferTest** app.
   - Delete the **mongo-sink** connector in the Control Center.
   - Then re-upload the **register-mongo-sink.json** file is located in the **Connectors** folder.

5. Run the **TransferTest** app to validate that data is flowing from Postgres to Kafka through an intermediary.
   - Set a breakpoint in `Program.Run_Producer`.
   - Press F5 to start the debugger.
   - Open **pgAdmin** and run the following SQL.
   ```sql
   INSERT INTO public.person(
      first_name, last_name, favorite_color, age, row_version)
      VALUES ('Donald', 'Duck', 'Blue', 11, now());
   ```
   - Open the Control Center to validate that a message was written to the `sink-person-data` topic.
   - Open **Mongo Express** to validate that a record was written to the `person` collection in `sink-database`.

## Worker

> **Note**: See this blog post explaining the design of the [event stream processing framework](https://blog.tonysneed.com/2020/06/25/event-stream-processing-micro-framework-apache-kafka/) used here.

1. Add a `TransformHandler` class to the **Handlers** folder.
   - Delete the **placeholder.txt** file.
   - Add **TransformHandler.cs** to the **Handlers** folder, with a public `TransformHandler` extending `MessageHandler` in the `Worker.Handlers` namespace.
   - Add a constructor accepting an `ILogger` that initializes a private `logger` field.
   - Override `HandleMessage` to transform source `Person` to sink `Person`.
     - Merge `first_name`, `last_name` into `name`.
   ```csharp
   public override async Task<Message> HandleMessage(Message sourceMessage)
   {
      // Convert message from source to sink format
      var message = (Message<Confluent.Kafka.Ignore, Protos.Source.v1.person>)sourceMessage;
      var key = new Protos.Sink.v1.Key
      {
         PersonId = message.Value.PersonId
      };
      var value = new Protos.Sink.v1.person
      {
         PersonId = message.Value.PersonId,
         Name = $"{message.Value.FirstName} {message.Value.LastName}",
         FavoriteColor = message.Value.FavoriteColor,
         Age = message.Value.Age,
         RowVersion = message.Value.RowVersion
      };

      // Call next handler
      var sinkMessage = new Message<Protos.Sink.v1.Key, Protos.Sink.v1.person>(key, value);
      logger.LogInformation($"Transform handler: {sinkMessage.Key} {sinkMessage.Value}");
      return await base.HandleMessage(sinkMessage);
   }
   ```
2. In `Program.Main` add async event processor.
   - First uncomment the type aliases at the top of the file.
   - Add code beneath the `// TODO` comment that adds a singleton of `IEventProcessorWithResult<Result>` to `services`.
   - Get the logger, create Kafka consumer and producer.
   - Return a new `KafkaEventProcessorWithResult`, passing in a new `TransformHandler`.
   ```csharp
   services.AddSingleton<IEventProcessorWithResult<Result>>(sp =>
   {
      // Get logger
      var logger = sp.GetRequiredService<ILogger>();

      // Create consumer, producer
      var kafkaConsumer = KafkaUtils.CreateConsumer<SourcePerson>(
         brokerOptions, consumerOptions.TopicsList, logger);
      var kafkaProducer = KafkaUtils.CreateProducer<SinkKey, SinkPerson>(
         brokerOptions, logger);

      // Return event processor using async producer
      return new KafkaEventProcessorWithResult<Ignore, SourcePerson, SinkKey, SinkPerson>(
         new KafkaEventConsumer<Ignore, SourcePerson>(kafkaConsumer, logger),
         new KafkaEventProducerAsync<SinkKey, SinkPerson>(kafkaProducer, producerOptions.Topic),
         new TransformHandler(logger));
   });
   ```
3. Edit **KafkaWorker.cs** to process event streams,
   - Uncomment the type alias at the top of the file.
   - Uncomment fields and constructor code.
   - In `KafkaWorker.ExecuteAsync` call `eventProcessor.ProcessWithResult`.
     - Uncomment logger code.
   ```csharp
   var deliveryResult = await eventProcessor.ProcessWithResult(cancellationToken);
   if (deliveryResult != null)
      logger.LogInformation($"delivered to: {deliveryResult.TopicPartitionOffset}");
   ```
4. Run the worker project in the debugger.
   - Set a breakpoint in both `KafkaWorker` and `TransformHandler`.
   - Validate that records flow from Postgres to MongoDB.
   - Add another record to Postgres to see it flow transformed to MongoDB.
   ```sql
   INSERT INTO public.person(
      first_name, last_name, favorite_color, age, row_version)
      VALUES ('Minnie', 'Mouse', 'Yellow', 12, now());
   ```
