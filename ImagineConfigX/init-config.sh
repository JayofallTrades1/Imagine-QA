#!/bin/bash
#
# Sets hostname addresses of all config files
# Uses script arguments ./init-config HOST=% AUTH=% MOTION=% RABBIT_URL=% AWS_REGION=% USE_TLS=%
#

for ARGUMENT in "$@"; do

    KEY=$(echo $ARGUMENT | cut -f1 -d=)
    VALUE=$(echo $ARGUMENT | cut -f2 -d=)

    case "$KEY" in
    HOST) HOST=${VALUE} ;;
    *) ;;
    esac

done

# Display input variables (for debug :))
echo "HOST = $HOST"

# Set Content and other configs
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/content/config.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/content/config.override.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/global/identityserver.auth.config.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/global/hermes.config.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/global/rabbit.config.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/global/SSConfig.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/hermes/appsettings.defaults.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/hermes/appsettings.overrides.json
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/mcs/configuration.json

# yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/core/docker-compose-core.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-dbschema.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-contentportal-config.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-vauto.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-vauto-webui-op1.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-vauto-webui-op2.yml
sed -i "s^\$HOSTNAME^$HOST^g" /etc/imagine/config/docker/vauto/docker-compose-auth-config.yml
