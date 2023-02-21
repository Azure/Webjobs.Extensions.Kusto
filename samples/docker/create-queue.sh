echo "Starting to run command and create queues"
sleep 10
rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue
if [ $? -ne 0 ];then
    echo "Creating queue failed, retrying in 30 seconds again"
    sleep 30
    rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue
fi