docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
docker run -p 6379:6379 --name some-redis -d redis redis-server  --loglevel warning