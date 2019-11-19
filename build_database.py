from model_download import Free3DDownloader
from unpack import unpack
from structure_3d_files import Structurer
from ply_to_csv import PlyCsvConverter
import subprocess

def download(objs_dir):
    downloader = Free3DDownloader(open_browser=True)
    categories = [line.rstrip("\n") for line in open("./categories.txt", "r")]
    for category in categories:
        downloader.download_category(category, objs_dir)
    downloader.quit()

def structure(objs_dir):
    s = Structurer()
    s.structure(objs_dir)
    
def convert(objs_dir):
    ply_csv_converter = PlyCsvConverter(objs_dir, max_points=10000)
    ply_csv_converter.start()
    
def update_blacklist():
    f = open("CrashLog.txt", "r")
    str = f.readline()
    strs = str.split(":")
    filename = strs[1][1:-1]
    f.close()
    
    f = open("blacklist.txt", "a")
    f.write("\n")
    f.write(filename)
    f.close()

if __name__ == "__main__":
    objects_dir = "./objects"
    compositor_dir = "./scene_compositor/Build/Compositor/Compositor.exe"
    download(objects_dir)
    unpack(objects_dir)
    structure(objects_dir)
    while True:
        try:
            p = subprocess.Popen([compositor_dir])
            convert(objects_dir)
        except ConnectionResetError:
            update_blacklist()
            continue
        except Exception as e:
            print(e)
            break
        p.terminate()
        break
