import numpy as np
import os
import argparse

def clean_points(file_dir, max_dim=3.5):
    points = np.loadtxt(file_dir, delimiter=";")
    idx_to_delete = np.where(np.absolute(points[:,:3]) > max_dim)
    cleaned_points = np.delete(points, idx_to_delete, axis=0)
    np.savetxt(file_dir, cleaned_points, delimiter=";")

def clean_scenes(objects_dir, max_dim=3.5):
    pc_dir = objects_dir + "/PointcloudScenes"
    files = os.listdir(pc_dir)
    i = 0
    for filename in files:
        if filename[-4:] != ".csv":
            continue
        file_dir = pc_dir + "/" + filename
        clean_points(file_dir, max_dim)
        i += 1
        print(100 * i / len(files), "%")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()

    # training environment parameters
    parser.add_argument("--objs_dir", type=str, default="./objects", help="Location of the 'PointcloudScenes' directory. Will be created if it not exists. Default: ./objects")
    parser.add_argument("--max_dim", type=float, default=3.5, help="If any point has a spatial coordinate (x,y,z) greater than max_dim, it will be removed from the point cloud. Default: 3.5")
    args = parser.parse_args()
    
    clean_scenes(args.objs_dir, args.max_dim)