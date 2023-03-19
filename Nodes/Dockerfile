FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
RUN apt-get update
RUN apt-get install default-jre -y
RUN apt-get install graphviz -y
RUN apt-get install gnuplot -y
RUN apt-get install wget -y
RUN apt-get install lbzip2 -y
RUN apt-get install git -y
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Nodes/Nodes.csproj", "Nodes/"]
RUN dotnet restore "Nodes/Nodes.csproj"
COPY . .
WORKDIR "/src/Nodes"
RUN dotnet build "Nodes.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nodes.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN wget github.com/jepsen-io/maelstrom/releases/download/v0.2.3/maelstrom.tar.bz2
RUN tar -xf maelstrom.tar.bz2
RUN rm maelstrom.tar.bz2
WORKDIR maelstrom

# Echo
# ./maelstrom test -w echo --bin /app/Nodes --node-count 1 --time-limit 10 

# Unique id generation
# ./maelstrom test -w unique-ids --bin /app/Nodes --time-limit 30 --rate 1000 --node-count 3 --availability total --nemesis partition 

# Single-Node Broadcast
# ./maelstrom test -w broadcast --bin /app/Nodes --node-count 1 --time-limit 20 --rate 10 

# Multi-Node Broadcast
# ./maelstrom test -w broadcast --bin /app/Nodes --node-count 5 --time-limit 20 --rate 10

# Fault Tolerant Broadcast
# ./maelstrom test -w broadcast --bin /app/Nodes --node-count 5 --time-limit 20 --rate 10 --nemesis partition

# Efficient Broadcast 1
# ./maelstrom test -w broadcast --bin /app/Nodes --node-count 25 --time-limit 20 --rate 100 --latency 100

# Grow only counter
# ./maelstrom test -w g-counter --bin /app/Nodes --node-count 3 --rate 100 --time-limit 20 --nemesis partition

# Single-node Kafka
# ./maelstrom test -w kafka --bin /app/Nodes --node-count 1 --concurrency 2n --time-limit 20 --rate 1000