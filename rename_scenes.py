import os

def rename_scenes(objects_dir):
	pc_dir = objects_dir + "/PointcloudScenes"
	i = 0
	for filename in os.listdir(pc_dir):
		new_filename = "scene_" + str(i) + ".csv"
		os.rename(pc_dir + "/" + filename, pc_dir + "/" + new_filename)
		i += 1
	
if __name__ == "__main__":
	objects_dir = "./objects"
	rename_scenes(objects_dir)