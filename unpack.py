from pyunpack import Archive
import os

def make_dir(dir):
	if os.path.isdir(dir) == False:
		os.makedirs(dir)
		
def trim_name(name):
	endings = [".zip", ".rar"]
	for ending in endings:
		if name[-len(ending):] == ending:
			return name[:-len(ending)]
	return name

def unpack(obj_dir = "./objects"):
	categories = [dI for dI in os.listdir(obj_dir) if os.path.isdir(os.path.join(obj_dir,dI))]
	for category in categories:
		directory = obj_dir + "/" + category
		unpack_dir = directory + "/unpacked"
		make_dir(unpack_dir)
		for filename in os.listdir(directory):
			file_dir = directory + "/" + filename
			# unpack_dir is not a zip file
			if file_dir == unpack_dir:
				continue
			file_unpack_dir = unpack_dir + "/" + trim_name(filename)
			make_dir(file_unpack_dir)
			# already unpacked
			if len(os.listdir(file_unpack_dir)) != 0:
				continue
			try:
				Archive(file_dir).extractall(file_unpack_dir)
				print(filename)
			except:
				print("error: " + filename)
	