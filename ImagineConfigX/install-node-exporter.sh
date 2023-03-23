#!/bin/bash
cd $HOME

URL="https://github.com/prometheus/node_exporter/releases/download/v1.1.2/node_exporter-1.1.2.linux-amd64.tar.gz"
PACKAGE="node_exporter-1.1.2.linux-amd64"

sudo yum install wget -y
sudo yum install dnf -y

sudo wget https://golang.org/dl/go1.15.6.linux-amd64.tar.gz
sudo tar -C /usr/local -xzf go1.15.6.linux-amd64.tar.gz
sudo sh -c "echo 'export PATH=$PATH:/usr/local/go/bin' >> /etc/profile"
source /etc/profile

#Install GO
echo "Installed Go: "
go version

## Download Package

sudo echo "Running wget: "
sudo wget "${URL}"

PKG_FOLDER="${PACKAGE}.tar.gz"
sudo tar xvfz "$PKG_FOLDER"

#Move Binaries

cd $HOME/${PACKAGE}
sudo mv node_exporter /usr/local/bin/
sudo useradd -rs /bin/false node_exporter

sudo sh -c 'echo "[Unit]
Description=Node Exporter
After=network.target

[Service]
User=node_exporter
Group=node_exporter
Type=simple
ExecStart=/usr/local/bin/node_exporter

[Install]
WantedBy=multi-user.target" >  /lib/systemd/system/node-exporter.service'

sudo echo "Wrote node-exporter service configuration"

sudo systemctl daemon-reload
sudo systemctl start node-exporter
sudo systemctl enable node-exporter
