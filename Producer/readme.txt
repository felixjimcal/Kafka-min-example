Build containers manually:
docker pull confluentinc/cp-zookeeper:latest
docker pull confluentinc/cp-kafka:latest
docker network create kafka_net
docker run -d --network=kafka_net --name=zookeeper -e ZOOKEEPER_CLIENT_PORT=2181 -e ZOOKEEPER_TICK_TIME=2000 -p 2181:2181 confluentinc/cp-zookeeper:latest
docker run -d --network=kafka_net --name=kafka -p 9092:9092 -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 confluentinc/cp-kafka:latest
----------------------------------------------------
Build containers automatically:
docker-compose up --build -d
----------------------------------------------------
<ItemGroup>
	  <PackageReference Include="Confluent.Kafka" Version="2.3.0" />
	  <PackageReference Include="FakerDotNet" Version="1.0.7" />
	  <PackageReference Include="kafka-sharp" Version="1.4.3" />
	  <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>
----------------------------------------------------
UI tool: https://www.conduktor.io/get-started/#desktop
----------------------------------------------------