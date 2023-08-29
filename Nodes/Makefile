echo:
	./maelstrom test -w echo --bin build/bin/Debug/net7.0/Nodes.exe --node-count 1 --time-limit 10 --log-stderr
	
id:
	./maelstrom test -w unique-ids --bin /app/Nodes --time-limit 30 --rate 1000 --node-count 3 --availability total --nemesis partition --log-stderr
	
singleBroadcast:
	./maelstrom test -w broadcast --bin /app/Nodes --node-count 1 --time-limit 20 --rate 10 --log-stderr

multiBroadcast:
	./maelstrom test -w broadcast --bin /app/Nodes --node-count 5 --time-limit 20 --rate 10 --log-stderr

faultyBroadcast:
	./maelstrom test -w broadcast --bin /app/Nodes --node-count 5 --time-limit 20 --rate 10 --nemesis partition --log-stderr

efficientBroadcast:
	./maelstrom test -w broadcast --bin /app/Nodes --node-count 25 --time-limit 20 --rate 100 --latency 100 --log-stderr

counter:
	./maelstrom test -w g-counter --bin /app/Nodes --node-count 3 --rate 100 --time-limit 20 --nemesis partition --log-stderr

singleKafka:
	./maelstrom test -w kafka --bin /app/Nodes --node-count 1 --concurrency 2n --time-limit 20 --rate 1000 --log-stderr