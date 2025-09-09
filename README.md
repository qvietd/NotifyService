docker run -d --name redis-server -p 6379:6379 redis
// exlain
-d: Detached mode (runs in background)
--name redis-server: Name the container
-p 6379:6379: Expose Redis port

docker run -d --name mongodb-server -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=secret mongo
// exlain
-e: Set environment variables
MONGO_INITDB_ROOT_USERNAME: MongoDB root username
MONGO_INITDB_ROOT_PASSWORD: MongoDB root password
-p 27017:27017: Expose MongoDB port

// connect CLI
docker exec -it redis-server redis-cli
docker exec -it mongodb-server mongosh -u admin -p secret

// connection string
no authen: mongodb://localhost:27017
authen: mongodb://admin:secret@localhost:27017

// rabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management

=> docker compose up -d

-- run this project
dotnet run --project .\NotifyService\NotifyService.csproj

-- show data in DB
docker exec -it mongodb-server mongosh
docker exec -it mongodb-server mongosh "mongodb://admin:secret@localhost:27017/?authSource=admin"
use mydb
db.collection.find()