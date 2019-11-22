from model_download import Free3DDownloader
from unpack import unpack
from structure_3d_files import Structurer
from ply_to_csv import PlyCsvConverter
import subprocess
import argparse 

def download(objs_dir):
    downloader = Free3DDownloader(open_browser=True)
    categories = [line.rstrip("\n") for line in open("./categories.txt", "r")]
    for category in categories:
        downloader.download_category(category, objs_dir)
    downloader.quit()

def structure(objs_dir):
    s = Structurer()
    s.structure(objs_dir)
    
def convert(objs_dir, max_points=10000):
    ply_csv_converter = PlyCsvConverter(objs_dir, max_points=max_points)
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

    parser = argparse.ArgumentParser()
    
    # training environment parameters
    parser.add_argument("--objs_dir", type=str, default="./objects", help="Location of the 'PointcloudScenes' directory. Will be created if it not exists. Default: ./objects")
    parser.add_argument("--compositor_dir", type=str, default="./scene_compositor/Build/Compositor/Compositor.exe", help="Directory to the builded scene_compositor program. Default: ./scene_compositor/Build/Compositor/Compositor.exe")
    parser.add_argument("--conversion_only", type=bool, default=False, help="If 'True', only a conversion of the mesh scenes to point clouds will be conducted and the download will be skipped. Default: False")
    parser.add_argument("--max_points", type=int, default=10000, help="Determines the maximum number of points in the resulting point clouds. Default: 10000")
    parser.add_argument("--debug", type=bool, default=False, help="If 'True', the unity compositor can be started from the unity editor. Default: False")
    
    args = parser.parse_args()
    
    print(args)
    
    if not args.conversion_only: # whole processing
        download(args.objs_dir)
        unpack(args.objs_dir)
        structure(args.objs_dir)
        
    while True:
        try:
            if not args.debug:
                p = subprocess.Popen([args.compositor_dir])
            convert(args.objs_dir, max_points=args.max_points)
        except ConnectionResetError:
            update_blacklist()
            continue
        except Exception as e:
            print(e)
            break
        if not args.debug:
            p.terminate()
        break
