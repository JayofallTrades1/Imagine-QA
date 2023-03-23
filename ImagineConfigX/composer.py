import argparse
import json
import requests
import fileinput

#### Handle Incoming Arguments
parser = argparse.ArgumentParser()
parser = argparse.ArgumentParser(description="Mangles Dockercompose files")
parser.add_argument("-o", "--output", help = "Output file")
parser.add_argument("-i", "--input", help = "Input File")
parser.add_argument("-r", "--region", help = "AWS Region")
parser.add_argument("-s", "--secret", help = "AWS Secret")
parser.add_argument("-p", "--hermes", help = "Hermes Host")
parser.add_argument("-w", "--websocket", help = "Websocket Host")
parser.add_argument("-k", "--identity", help = "Keycloak Host")
parser.add_argument("-c", "--cloudtool", help = "Spinup Host")
 
# Read arguments from command line
args = parser.parse_args()

manifest_path = '/etc/imagine/config/manifest.json'
template_file = args.input
global filedata

try:
    f = open(manifest_path,)
except FileNotFoundError:
    print("ERROR: rabbit.config.json file not found in "+file_path+", check filename or file path")
    raise SystemExit()

manifest = json.load(f)
#print(manifest["spinup"]["version"])

def openfile(filename):
    global filedata
    with open(filename, 'r') as file :
       filedata = file.read()

def replace(StringToReplace,NewText):
    global filedata
    filedata = filedata.replace(StringToReplace,NewText)

def writefile():
    with open(args.output, 'w') as file:
       file.write(filedata)
       print("Composer wrote file "+args.output)

openfile(template_file)
## Versions ##
replace("$VER_CHANNEL-SPINUP",manifest["channel-spinup"]["version"])
replace("$VER_VAUTO-SETTINGS-UI",manifest["vauto-settings-ui"]["version"])
replace("$VER_EVOYPROXY",manifest["envoy-dev"]["version"])

## Other Variables ##
replace("$VAR_AWS_REGION",args.region)
replace("$VAR_AWS_SECRET",args.secret)
replace("$VAR_SPINUP_SERVICE_HOSTNAME",args.cloudtool)
replace("$VAR_IDENTITY_SERVER",args.identity)
replace("$VAR_HERMES_HOSTNAME",args.hermes)
replace("$VAR_WEBSOCKET_HOSTNAME",args.websocket)

writefile()