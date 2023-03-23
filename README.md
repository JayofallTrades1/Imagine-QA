# Imagine QA

This is a private repo where I took a Production hybrid(on-prem/cloud) system and created a QA system fully in the cloud. This was using Imagine Communcations Versio Automation Media Asset Management software. 

The terraform config that I wrote will easily standup/breakdown the system at will. Golden AMIs have been created within AWS in a private repo under AMC's account. This repo will serve as a mere reference and should not be duplcated as licenses will need to be bought. 

The architecture of the system is shown in the diagram below:

![Untitled](https://user-images.githubusercontent.com/31706314/227219936-6279fa4f-1c6f-45aa-99c1-5ffe485147c4.png)


Apply the rabbit consul stacks
docker stack deploy -c /etc/imagine/config/docker/rabbit/docker-compose-consul.yml Consul
docker exec into one of them and run consul members or consul operator raft list-peers and it should show a leader of the 6 nodes.

Apply the rabbit stacks
docker stack deploy -c /etc/imagine/config/docker/rabbit/docker-compose-rabbit.yml Rabbit
Log into the rabbit ui to make sure the cluster is created. via http://node:15672

Apply the hermes stack
docker stack deploy -c /etc/imagine/config/docker/core/docker-compose-hermes.yml Hermes --with-registry-auth
check hermes is running via http://vip:9872 on both sides, can also check haproxy stats that hermes came alive

Apply the keycloak stack
docker stack deploy -c /etc/imagine/config/docker/core/docker-compose-core-keycloak-op1.yml Keycloak-op1 --with-registry-auth
docker stack deploy -c /etc/imagine/config/docker/core/docker-compose-core-keycloak-op2.yml Keycloak-op2 --with-registry-auth
Check keycloak ui works via http://vip:8081/auth/ on both sides

Apply the one time runs:
docker-compose -f /etc/imagine/config/docker/vauto/docker-compose-auth-config.yml up -d
docker-compose -f /etc/imagine/config/docker/vauto/docker-compose-contentportal-config.yml up -d
docker-compose -f /etc/imagine/config/docker/vauto/docker-compose-dbschema.yml up -d
Check "docker logs <containerid>" on each of these to make sure they all ran successfully.
Must make sure all of these work prior to moving on to next step

Setup rabbit virtual host and policies:
in the init-rabbitmq.sh script it says on line 6/etc/imagine/config/global/rabbit.config.json (So make sure this is pointed to e.g. x side rabbit or over haproxy if it is available)

have python installed and run:
python2 /etc/imagine/config/init-rabbitmq.py --mode standard

Apply the Vauto backend stack
docker stack deploy -c /etc/imagine/config/docker/vauto/docker-compose-vauto.yml Vauto-Stack --with-registry-auth
Check haproxy stats for green backends contentservice, mcp, vauto, heartbeat etc

Apply the Vauto webui frontend stacks
docker stack deploy -c /etc/imagine/config/docker/vauto/docker-compose-vauto-webui-op1.yml Vauto-Webui-OP1 --with-registry-auth
docker stack deploy -c /etc/imagine/config/docker/vauto/docker-compose-vauto-webui-op2.yml Vauto-Webui-OP2 --with-registry-auth
Check the vauto , cp ui pages ... and haproxy stats ... all should be green now.

