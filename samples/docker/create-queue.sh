sleep 10
apk --no-cache add curl
#rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue
curl -s -i -u guest:guest http://localhost:5672/api/queues/vhost/bindings.test.queue