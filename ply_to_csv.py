import numpy as np
from pyntcloud import PyntCloud
import sys
from mpl_toolkits.mplot3d import Axes3D
import matplotlib.pyplot as plt
import os
import pandas as pd
from math import floor
from server import Server

class PlyCsvConverter:
	def __init__(self, objs_dir, max_points=10000):
		self.objs_dir = objs_dir
		self.max_points = max_points

	def start(self):
		m_server = Server(message_handler=self.on_data_received)
		m_server.start_listen()

	def set_proper_aspect_ratio(self, ax):
		extents = np.array([getattr(ax, 'get_{}lim'.format(dim))() for dim in 'xyz'])
		sz = extents[:,1] - extents[:,0]
		centers = np.mean(extents, axis=1)
		maxsize = max(abs(sz))
		r = maxsize/2
		for ctr, dim in zip(centers, 'xyz'):
			getattr(ax, 'set_{}lim'.format(dim))(ctr - r, ctr + r)

	def plot_point_cloud(self, points):
		fig = plt.figure()
		ax = fig.add_subplot(111, projection="3d")
		ax.scatter(points[:,0], points[:,1], points[:,2], marker=".",color="blue")
		self.set_proper_aspect_ratio(ax)
		plt.show()

	def on_data_received(self, data):
		message = data.decode("utf-8")
		print("received: ", message)
		msg_parts = message.split(";")
		current_scene = msg_parts[0]
		area_sum = float(msg_parts[1])
        
		all_points = np.zeros((0,7))
		dir = self.objs_dir +  "/PointcloudScenes/Scene_" + current_scene
		df = pd.read_csv(dir + "/scene.csv", sep=";")
		plot_room = False

		labels = {}
		label_nr = 0
		# count the nr of objects
		for index, row in df.iterrows():
			filename = row["Filename"]
			ply_filename = filename[:-9]
			if ply_filename not in labels:
				labels[ply_filename] = label_nr
				label_nr += 1
		# transform .ply's to point clouds
		point_sum = 0
		for index, row in df.iterrows():
			filename = row["Filename"]
			name = filename[:-9]
			hasNormals = str(row["HasNormals"]) == "True"
			hasColors = str(row["HasColors"]) == "True"
			area = float(row["Area"])
			path = dir + "/" + filename
			
			# read the .ply and sample a point cloud from the mesh
			try:
				cloud = PyntCloud.from_file(path)
			except:
				print("error loading file '" + path + "'")
				return bytearray("error", "utf-8")	
                
			n_points = int((area/area_sum) * self.max_points)
			point_sum += n_points
			try:
				df = cloud.get_sample("mesh_random", n=n_points, rgb=hasColors, normals=hasNormals)
			except:
				print("error sampling data")
				return bytearray("error", "utf-8")
				
			# create a numpy array
			points = df[["x", "y", "z"]].values
			try:
				normals = df[["nx", "ny", "nz"]].values
			except:
				print("error accessing normals")
				return bytearray("error", "utf-8")
			label = labels[name]
			label_mat = np.full((points.shape[0], 1), label)
			points = np.hstack((points, normals, label_mat))
            
			# add the points of one object to a matrix which represents the scene of multiple objects
			all_points = np.vstack((all_points, points))
			os.remove(path)

        # save point cloud, clean up and respond to request
		os.remove(dir + "/scene.csv")
		os.rmdir(dir)
		np.savetxt(self.objs_dir + "/PointcloudScenes/scene_" + current_scene + ".csv", all_points, delimiter=";")
		return bytearray("done", "utf-8")

if __name__ == "__main__":
	objects_dir = "./objects"
	ply_csv_converter = PlyCsvConverter(objects_dir)
	ply_csv_converter.start()
