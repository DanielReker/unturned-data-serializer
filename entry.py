import subprocess
import os
import json
import time


def load_config():
    try:
        with open("/app/configs/config.json") as file:
            return json.load(file)
    except:
        with open("/app/default_configs/config.json") as file:
            return json.load(file)
        

cfg = load_config()
server_name = cfg['serverName']

def clean_output():
    print("Cleaning output directory...")
    subprocess.run("rm -r /app/output/*", shell=True)

def update_server():
    print("Updating server...")
    subprocess.run("/home/steam/steamcmd/steamcmd.sh +force_install_dir /app/U3DS +login anonymous +app_update 1110390 +quit", shell=True)
    
def install_module():
    print("Installing UnturnedDataSerializer module...")
    subprocess.run("cp -rf /app/modules/UnturnedDataSerializer /app/U3DS/Modules/", shell=True)

def run_server(map_cfg):
    map_name = map_cfg['name']

    os.makedirs(f'/app/U3DS/Servers/{server_name}/Server/', exist_ok=True)

    with open(f'/app/default_configs/WorkshopDownloadConfig.json', 'r') as file:
        workshop_cfg = json.load(file)
    workshop_cfg['File_IDs'] = map_cfg['workshopIDs']
    with open(f'/app/U3DS/Servers/{server_name}/WorkshopDownloadConfig.json', 'w+') as file:
        json.dump(workshop_cfg, file)
    
    with open(f'/app/U3DS/Servers/{server_name}/Server/Commands.dat', 'w+') as file:
        file.write(f'Map {map_name}')

    subprocess.run(f'cd /app/U3DS && ./ServerHelper.sh +LanServer/{server_name}', shell=True)

def generate_metadata():
    with open('/app/U3DS/Status.json', 'r') as file:
        status = json.load(file)

    metadata = {
        'timestamp': int(time.time()),
        'gameVersion': {
            'major': status['Game']['Major_Version'],
            'minor': status['Game']['Minor_Version'],
            'patch': status['Game']['Patch_Version']
        },
        'availableMaps': [],
        'status' : 'success'
    }

    for map_cfg in cfg['maps']:
        map_name = map_cfg['name']
        if os.path.isdir(f'/app/output/Maps/{map_name}/'):
            metadata['availableMaps'].append(map_name)
        else:
            metadata['status'] = 'fail'

    with open('/app/output/metadata.json', 'w+') as file:
        json.dump(metadata, file)


clean_output()
update_server()
install_module()
for map_cfg in cfg['maps']:
    run_server(map_cfg)
generate_metadata()