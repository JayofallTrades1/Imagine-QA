docker stack rm Vauto-Stack 
echo "sleeping 10"
sleep 10
docker stack deploy -c /etc/imagine/vauto/docker-compose-vauto.yml Vauto-Stack --with-registry-auth
docker ps -a | grep Vauto-Stack

