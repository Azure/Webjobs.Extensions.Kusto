echo "Starting to run command and create queues"
sleep 10
rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue
if [ $? -ne 0 ];then
    echo "Creating queue failed, retrying in 30 seconds again"
    sleep 30
    rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue
fi
#apk --no-cache add curl
#curl -s -i -u guest:guest http://localhost:15672/api/queues/vhost/bindings.test.queue
# #!/bin/bash
# echo "Starting to run command and create queues. Will be retried 5 times in case of not being successful"
# max_retry=5
# counter=0
# until `rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue`
# do
#    sleep 15
#    [[ counter -eq $max_retry ]] && echo "Failed!" && exit 1
#    echo "Trying again. Try #$counter"
#    ((counter++))
# done