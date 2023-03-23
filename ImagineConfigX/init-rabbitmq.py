import argparse
import json
import requests

# Defaults everything else it pulled from the rabbitconfig.json file
file_path = '/etc/imagine/config/global/rabbit.config.json'
port = 15672
headers = {'content-type':'application/json'}
policy_name = "Imagine"

################################## Imagine Rabbit Policies ########################################

imagine_policy = {
           "vhost":"Versio",
           "name":"GeneralImagine",
           "pattern":".*",
           "apply-to":"all",
           "definition":{
               "expires":30000,
               "max-length":1000,
               "message-ttl":30000,
               "dead-letter-exchange":"mydlx"
            },
               "priority":1
           }

imagine_ha_policy = {
           "vhost":"Versio",
           "name":"GeneralImagine",
           "pattern":".*",
           "apply-to":"all",
           "definition":{
               "expires":30000,
               "max-length":1000,
               "message-ttl":30000,
               "dead-letter-exchange":"mydlx"
            },
               "priority":1
           }

##########################################################################

# Get full command-line arguments
parser = argparse.ArgumentParser()
parser = argparse.ArgumentParser(description="Configures Imagine RabbitMQ or AmazonMQ configuration, reads the rabbit.config.json file and applies configuration to the host \
                                              through the management API. Use mode to set operation 'aws' (default) is used to configure an AmazonMQ rabbit service.\
                                              'standard' is used for normal on-prem rabbit or none TLS rabbit container \
                                              ")
parser.add_argument("-m", "--mode", help = "Config as 'standard' or 'aws'. Default: aws")
parser.add_argument("-f", "--file", help = "RabbitMQ rabbit.config.json path override. Default: /etc/imagine/config/global")
parser.add_argument("-p", "--port", help = "RabbitMQ management API port. Default: standard mode :15672 http, aws mode :442 https")
 
# Read arguments from command line
args = parser.parse_args()
standard_mode =False
 
if(args.mode == "standard"):
    print("Configuration Mode: % s" % args.mode)
    standard_mode = True
if(args.file):
    print("File path set to: % s" % args.file)
    file_path = args.file

if(args.port):
    print("Port set to: % s" % args.port)
    port = args.port

# Opening rabbit.confg.json file to get the configured rabbit 

try:
    f = open(file_path,)
except FileNotFoundError:
    print("ERROR: rabbit.config.json file not found in "+file_path+", check filename or file path")
    raise SystemExit()

config = json.load(f)

if(standard_mode):
   url = "http://"+config["Host"]+":"+str(port)
else:
   url = url = "https://"+config["Host"]


## REST call functions


def api_get(request):
    try:
        response = requests.get(
            url+request,
            headers=headers,
            auth=(config["UserName"],config["Password"]),
        )
    except requests.exceptions.RequestException as e:  
        print("ERROR: An Error has occured, likely could not connect to host "+config["Host"])    
        raise SystemExit(e)
    
    return response


def api_put(request,payload):
    try:
        response = requests.put(
            url+request,
            headers=headers,
            auth=(config["UserName"],config["Password"]),
            json=payload
    )
    except requests.exceptions.RequestException as e:  
        print("ERROR: Rabbit API Put Request, An Error has occured")    
        raise SystemExit(e)

    return response

#### Base API Calls ####

def add_vhost(vhostname):
    response = api_put("/api/vhosts/"+vhostname,None)
    return response

def add_policy(vhost,pname,policy):
    api_put("/api/policies/"+vhost+"/"+pname,policy)
    return response

def get_cluster_size():
    response = api_get("/api/nodes")
    return response

def get_cluster_name():
    response = api_get("/api/cluster-name")
    return response

def process_codes(status_code):
    if(response.status_code == 200):
        print("200: Success")
    elif(response.status_code == 201):
        print("201: Created")
    elif(response.status_code == 204):
        print("204: Already Exists")
    elif(response.status_code == 401):
        print("ERROR: Unauthorized, please check username and password")
        raise SystemExit()
    else:
        print(response.status_code+": I wasn't expecting that response code")

## Check to see if you get a response from host
print("Checking for a Rabbit at "+config["Host"])
response = get_cluster_name()
process_codes(response)
data = json.loads(response.text)
print("Found Cluster: "+data["name"])
## Add virtual host
print("Adding Virtual Host: "+config["VirtualHost"])
process_codes(add_vhost(config["VirtualHost"]))
## Check to see if HA system
print("Checking Number of Nodes")
response = get_cluster_size()
process_codes(response)
data = json.loads(response.text)
node_count = len(data)
print("Number of Nodes: "+str(node_count))

## Apply the right policies

if(node_count > 1):
    print("Adding Imagine specific policies for HA systems")
    process_codes(add_policy(config["VirtualHost"],policy_name,imagine_ha_policy))   
else:
    print("Adding Imagine specific policies for standalone systems") 
    process_codes(add_policy(config["VirtualHost"],policy_name,imagine_policy))
     
# Work done closing config file
f.close()
