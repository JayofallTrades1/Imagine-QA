pcs resource create postgresql systemd:postgresql.service
pcs resource create content systemd:content.service
pcs resource create heartbeat systemd:heartbeat.service
pcs resource create hermes systemd:hermes.service
pcs resource create mastercontrol systemd:mastercontrol.service
pcs resource create motion systemd:motion.service
pcs resource create rabbitmq systemd:rabbitmq.service
pcs resource create ui systemd:ui.service
pcs resource create websocket systemd:websocket.service
#pcs resource create sql systemd:sql.service
pcs resource create keycloak systemd:keycloak.service
pcs resource create integration systemd:integration.service
pcs resource create httpbridge systemd:httpbridge.service
pcs resource create ingest systemd:ingest.service

pcs resource create vip IPaddr2 ip=10.121.205.100 cidr_netmask=24 arp_bg=true arp_count=10 arp_interval=200

pcs constraint colocation add postgresql with vip INFINITY
pcs constraint colocation add content with vip INFINITY
pcs constraint colocation add heartbeat with vip INFINITY
pcs constraint colocation add hermes with vip INFINITY
pcs constraint colocation add mastercontrol with vip INFINITY
pcs constraint colocation add motion with vip INFINITY
pcs constraint colocation add rabbitmq with vip INFINITY
pcs constraint colocation add ui with vip INFINITY
pcs constraint colocation add websocket with vip INFINITY
#pcs constraint colocation add sql with vip INFINITY
pcs constraint colocation add keycloak with vip INFINITY
pcs constraint colocation add ingest with vip INFINITY
pcs constraint colocation add httpbridge with vip INFINITY
pcs constraint colocation add integration with vip INFINITY

pcs resource cleanup postgresql
pcs resource cleanup content
pcs resource cleanup heartbeat
pcs resource cleanup hermes
pcs resource cleanup mastercontrol
pcs resource cleanup motion
pcs resource cleanup rabbitmq
pcs resource cleanup ui
pcs resource cleanup websocket
pcs resource cleanup keycloak
pcs resource cleanup ingest
