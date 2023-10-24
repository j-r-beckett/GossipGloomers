echo:
	./maelstrom test -w echo --bin build/bin/Debug/net7.0/Nodes.exe --node-count 1 --time-limit 10 --log-stderr
	
id:
	./maelstrom test -w unique-ids --bin build/bin/Debug/net7.0/Nodes.exe --time-limit 30 --rate 1000 --node-count 3 --availability total --nemesis partition --log-stderr
	
singleBroadcast:
	./maelstrom test -w broadcast --bin build/bin/Debug/net7.0/Nodes.exe --node-count 1 --time-limit 20 --rate 10 --log-stderr

multiBroadcast:
	./maelstrom test -w broadcast --bin build/bin/Debug/net7.0/Nodes.exe --node-count 5 --time-limit 20 --rate 10 --log-stderr 

faultyBroadcast:
	./maelstrom test -w broadcast --bin build/bin/Debug/net7.0/Nodes.exe --node-count 5 --time-limit 20 --rate 10 --nemesis partition --log-stderr

efficientBroadcast:
	./maelstrom test -w broadcast --bin build/bin/Debug/net7.0/Nodes.exe --node-count 25 --time-limit 20 --rate 100 --latency 100 --log-stderr

counter:
	./maelstrom test -w g-counter --bin build/bin/Debug/net7.0/Nodes.exe --node-count 3 --rate 100 --time-limit 20 --nemesis partition --log-stderr

singleKafka:
	./maelstrom test -w kafka --bin build/bin/Debug/net7.0/Nodes.exe --node-count 1 --concurrency 2n --time-limit 20 --rate 1000 --log-stderr
	
multiKafka:
	./maelstrom test -w kafka --bin build/bin/Debug/net7.0/Nodes.exe --node-count 2 --concurrency 2n --time-limit 20 --rate 1000 --log-stderr
	
singleTransaction:
	./maelstrom test -w txn-rw-register --bin build/bin/Debug/net7.0/Nodes.exe --node-count 1 --time-limit 20 --rate 1000 --concurrency 2n --consistency-models read-uncommitted --availability total --log-stderr
	
multiTransaction:
	./maelstrom test -w txn-rw-register --bin build/bin/Debug/net7.0/Nodes.exe --node-count 2 --concurrency 2n --time-limit 20 --rate 1000 --consistency-models read-uncommitted --log-stderr
	
multiTransactionAvailability:
	./maelstrom test -w txn-rw-register --bin build/bin/Debug/net7.0/Nodes.exe --node-count 2 --concurrency 2n --time-limit 20 --rate 1000 --consistency-models read-uncommitted --availability total --nemesis partition --log-stderr