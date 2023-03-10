FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
RUN apt-get update
RUN apt-get install default-jre -y
RUN apt-get install graphviz -y
RUN apt-get install gnuplot -y
RUN apt-get install wget -y
RUN apt-get install lbzip2
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
RUN wget https://github.com/jepsen-io/maelstrom/releases/download/v0.2.3/maelstrom.tar.bz2
RUN tar -xf maelstrom.tar.bz2
RUN rm maelstrom.tar.bz2
WORKDIR maelstrom
#ENTRYPOINT ["dotnet", "Nodes.dll"]
./maelstrom test -w echo --bin /app/Nodes --time-limit 5 --log-stderr