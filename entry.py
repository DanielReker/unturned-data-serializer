import re
import shutil
import subprocess
import os
import json
import time

def log(msg):
    print(f'entry.py: {msg}', flush=True)

def load_config():
    try:
        with open("/app/configs/config.json") as file:
            return json.load(file)
    except:
        with open("/app/default_configs/config.json") as file:
            return json.load(file)

cfg = load_config()
server_name = cfg['serverName']

maps_list = [ map_cfg['name'] for map_cfg in cfg['maps'] ]

all_workshop_IDs = []
for map_cfg in cfg['maps']:
    all_workshop_IDs += map_cfg['workshopIDs']
log(f'All maps direct dependencies: {all_workshop_IDs}')

def load_versions():
    try:
        with open("/app/output/versions.json") as file:
            return json.load(file)
    except:
        return None


def update_server():
    log("Updating server...")
    subprocess.run("/home/steam/steamcmd/steamcmd.sh +force_install_dir /app/U3DS +login anonymous +app_update 1110390 +quit", shell=True)

def install_module():
    log("Installing UnturnedDataSerializer module...")
    subprocess.run("cp -rf /app/modules/UnturnedDataSerializer /app/U3DS/Modules/", shell=True)

def remove_map_output(map_name):
    log(f"Removing {map_name} output...")
    subprocess.run(f"rm -r '/app/output/Maps/{map_name}/'", shell=True)


def generate_tiles_for_map(map_name, map_type):
    log(f"Generating tiles for {map_name} {map_type}...")
    map_dir = f"/app/output/Maps/{map_name}"
    # TODO: Try to make image re-encoding more efficient
    subprocess.run(f"gdal_translate -of png -expand rgba '{map_dir}/{map_type}.png' /tmp/temp.png", shell=True)
    subprocess.run(f"[ -f /tmp/temp.png ] && mv -f /tmp/temp.png '{map_dir}/{map_type}.png'", shell=True)
    subprocess.run(f"gdal2tiles.py -p raster --xyz '{map_dir}/{map_type}.png' '{map_dir}/{map_type}/'", shell=True)

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

def run_server(map_name, workshop_IDs, mode):
    os.makedirs(f'/app/U3DS/Servers/{server_name}/Server/', exist_ok=True)

    with open(f'/app/default_configs/WorkshopDownloadConfig.json', 'r') as file:
        workshop_cfg = json.load(file)
    workshop_cfg['File_IDs'] = workshop_IDs
    with open(f'/app/U3DS/Servers/{server_name}/WorkshopDownloadConfig.json', 'w+') as file:
        json.dump(workshop_cfg, file)

    with open(f'/app/U3DS/Servers/{server_name}/Server/Commands.dat', 'w+') as file:
        file.write(f'Map {map_name}')

    dataSerializerConfig = {
        'mode': mode
    }
    with open('/app/dataSerializerConfig.json', 'w+') as file:
        json.dump(dataSerializerConfig, file)

    log(f"Running server for {map_name}")
    subprocess.run(f'cd /app/U3DS && ./ServerHelper.sh +LanServer/{server_name}', shell=True)

def fetch_map_data(map_cfg):
    map_name = map_cfg['name']

    remove_map_output(map_name)

    run_server(map_name, map_cfg['workshopIDs'], "serializeData")

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

def clean_output():
    directory = '/app/output/Maps'
    for name in os.listdir(directory):
        path = os.path.join(directory, name)
        if os.path.isfile(path):
            os.remove(path)
        elif name not in maps_list:
            shutil.rmtree(path)

def get_updated_maps():
    old_versions = load_versions()
    run_server("PEI", all_workshop_IDs, "getUpdatesInfo")
    versions = load_versions()

    if old_versions is None or versions is None:
        return maps_list

    def is_updated(item):
        if item not in versions:
            log(f'Item {item} was not found in versions.json')
            return True

        if 'is_updated' in versions[item]:
            return versions[item]['is_updated']

        if item not in old_versions or old_versions[item]['version'] != versions[item]['version'] or old_versions[item]['version'] != versions[item]['version']:
            versions[item]['is_updated'] = True
            return True

        for dependency in versions[item]['dependencies']:
            if is_updated(dependency):
                versions[item]['is_updated'] = True
                return True

        versions[item]['is_updated'] = False
        return False

    def is_any_updated(items):
        for item in items:
            if is_updated(str(item)):
                return True
        return False

    if is_updated('Unturned'):
        return maps_list

    return [ map_cfg['name'] for map_cfg in cfg['maps'] if is_any_updated(map_cfg['workshopIDs'] + [ 'Unturned' ]) ]

update_server()
install_module()
clean_output()
updated_maps = get_updated_maps()
log(f'Updated maps: {updated_maps}')
for map_cfg in cfg['maps']:
    if map_cfg['name'] in updated_maps:
        fetch_map_data(map_cfg)
generate_metadata()