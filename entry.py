import re
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


def update_server():
    print("Updating server...")
    subprocess.run("/home/steam/steamcmd/steamcmd.sh +force_install_dir /app/U3DS +login anonymous +app_update 1110390 +quit", shell=True)
    
def install_module():
    print("Installing UnturnedDataSerializer module...")
    subprocess.run("cp -rf /app/modules/UnturnedDataSerializer /app/U3DS/Modules/", shell=True)

def remove_map_output(map_name):
    print(f"Removing {map_name} output...")
    subprocess.run(f"rm -r '/app/output/Maps/{map_name}/'", shell=True)


def generate_tiles_for_map(map_name, map_type):
    print(f"Generating tiles for {map_name} {map_type}...")
    map_dir = f"/app/output/Maps/{map_name}"
    subprocess.run(f"/usr/bin/gdal_translate -of vrt -expand rgba '{map_dir}/{map_type}.png' /app/temp.vrt", shell=True)    
    subprocess.run(f"/usr/bin/gdal2tiles.py -p raster --xyz /app/temp.vrt '{map_dir}/{map_type}/'", shell=True)    

    # Save grid info as JSON
    # TODO: Rewrite grid info fetching
    with open(f'{map_dir}/{map_type}/openlayers.html', 'r') as file:
        html = file.read()
    to_find = "tileGrid: new ol.tilegrid.TileGrid("
    idx_begin = html.find(to_find) + len(to_find)
    html = html[idx_begin::]
    html = html[:html.find(')'):]
    html = re.sub(r'(\w+):', r'"\g<1>":', html)
    
    grid = json.loads(html)
    with open(f'{map_dir}/{map_type}/grid.json', 'w+') as file:
        json.dump(grid, file, indent=4)

    subprocess.run(f"rm '{map_dir}/{map_type}/openlayers.html'", shell=True)

def run_server(map_cfg):
    map_name = map_cfg['name']
    

    remove_map_output(map_name)
    
    os.makedirs(f'/app/U3DS/Servers/{server_name}/Server/', exist_ok=True)

    with open(f'/app/default_configs/WorkshopDownloadConfig.json', 'r') as file:
        workshop_cfg = json.load(file)
    workshop_cfg['File_IDs'] = map_cfg['workshopIDs']
    with open(f'/app/U3DS/Servers/{server_name}/WorkshopDownloadConfig.json', 'w+') as file:
        json.dump(workshop_cfg, file)
    
    with open(f'/app/U3DS/Servers/{server_name}/Server/Commands.dat', 'w+') as file:
        file.write(f'Map {map_name}')

    print(f"Running server for {map_name}")
    
    subprocess.run(f'cd /app/U3DS && ./ServerHelper.sh +LanServer/{server_name}', shell=True)
    
    generate_tiles_for_map(map_name, 'Map')
    generate_tiles_for_map(map_name, 'Chart')
    

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
        json.dump(metadata, file, indent=4)


update_server()
install_module()
for map_cfg in cfg['maps']:
    run_server(map_cfg)
generate_metadata()