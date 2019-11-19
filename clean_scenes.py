import numpy as np
import os

def clean_points(file_dir, max_dim=3.5):
	points = np.loadtxt(file_dir, delimiter=";")
	idx_to_delete = np.where(np.absolute(points[:,:3]) > max_dim)
	cleaned_points = np.delete(points, idx_to_delete, axis=0)
	np.savetxt(file_dir, cleaned_points, delimiter=";")

def clean_scenes(objects_dir):
	pc_dir = objects_dir + "/PointcloudScenes"
	for filename in os.listdir(pc_dir):
		if filename[-4:] != ".csv":
			continue
		file_dir = pc_dir + "/" + filename
		clean_points(file_dir)

if __name__ == "__main__":
	objs_dir = "./objects"
	clean_scenes(objs_dir)