import subprocess
import os
import json


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

clean_output()
update_server()
install_module()

for map_cfg in cfg['maps']:
    run_server(map_cfg)