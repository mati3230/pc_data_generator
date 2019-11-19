import os
from pyunpack import Archive
import pandas as pd
import numpy as np

class Structurer:
	def __init__(self):

		self.df = pd.DataFrame(columns=["Category", "FileName", "FileDir", "Material", "OtherFiles(FileName,Dir)"])

		self.other_file_sep = ","

		self.extensions_3d = ["obj", "fbx", "blend", "3ds", "dae", "stl", "ply", "c4d", "ma", "max", "atl", "dae", "ply", "unitypackage", "mb", "dxf", "chan", "x", "x3d", "abc"]

		self.pack_extensions = ["zip", "rar"]
		
		self.mat_extensions = ["mtl"]

		self.n_3d_files = 0
		self.iter = 0
		
	def is_file_extension(self, ext, exts):
		for extension in exts:
			if ext == extension:
				return True
		return False
		
	def make_dir(self, dir):
		if os.path.isdir(dir) == False:
			os.makedirs(dir)
	
	def add_information(self, info, col):
		if self.n_3d_files == 0: # one empty entry
			self.df[col][self.iter] = info
		return
		for i in range(self.n_3d_files):
			self.df[col][self.iter + i] = info
	
	def store_other_files(self, file, obj_dir, category):
		other_files = self.df["OtherFiles(FileName,Dir)"][self.iter] # first entry
		if len(file) == 0: # just copy
			information = ""
		else:
			information = file + self.other_file_sep + obj_dir
		if len(other_files) == 0:
			self.add_information(category, "Category")
			self.add_information(information, "OtherFiles(FileName,Dir)")
		else: 
			if self.n_3d_files == 0:
				self.df["Category"][self.iter] = category
				if len(information) == 0:
					self.df["OtherFiles(FileName,Dir)"][self.iter] = other_files
				else:
					self.df["OtherFiles(FileName,Dir)"][self.iter] = other_files + self.other_file_sep + information
				return
			for i in range(self.n_3d_files):
				self.df["Category"][self.iter + i] = category
				if len(information) == 0:
					self.df["OtherFiles(FileName,Dir)"][self.iter+i] = other_files
				else:
					self.df["OtherFiles(FileName,Dir)"][self.iter+i] = other_files + self.other_file_sep + information
			
	def unpack_sub_files(self, file, obj_dir, ext):
		file_unpack_dir = obj_dir[:-len(ext)]
		#print(file_unpack_dir)
		# already unpacked
		if os.path.isdir(file_unpack_dir):
			return file_unpack_dir
		self.make_dir(file_unpack_dir)
		Archive(obj_dir).extractall(file_unpack_dir)
		return file_unpack_dir
		
	# recursive search of 3d files of specific obj
	def search_3d_file(self, category, obj_dir):
		for dir in os.listdir(obj_dir):
			#print(obj_dir + "/" + dir)
			if os.path.isfile(obj_dir + "/" + dir):
				file = dir
				file_names = file.split(".")
				if len(file_names) == 0:
					# file without ending - skip
					continue
				ext = file_names[-1]
				if self.is_file_extension(ext, self.extensions_3d):
					self.n_3d_files = self.n_3d_files + 1
					#print(file)
					self.df["Category"][len(self.df)-1] = category
					self.df["FileName"][len(self.df)-1] = file
					self.df["FileDir"][len(self.df)-1] = obj_dir + "/" + file
					self.store_other_files("", "", category)
					self.df.loc[len(self.df)] = ["","","","",""]
					continue
				if self.is_file_extension(ext, self.pack_extensions):
					file_unpack_dir = self.unpack_sub_files(file, obj_dir + "/" + dir, ext)
					self.search_3d_file(category, file_unpack_dir)
					continue
				if self.is_file_extension(ext, self.mat_extensions):
					#print(file)
					self.add_information(obj_dir + "/" + file, "Material")
					continue
				# file could be texture or material
				self.store_other_files(file, obj_dir, category)
			else: # dir is a folder
				self.search_3d_file(category, obj_dir + "/" + dir) 
	
	def structure(self, obj_dir):
		categories = os.listdir(obj_dir)
		for category in categories:
			directory = "./objects/" + category
			if os.path.isfile(directory):
				continue
			unpack_dir = directory + "/unpacked"
			for obj_dir in os.listdir(unpack_dir):
				# append a new column
				self.n_3d_files = 0
				self.df.loc[len(self.df)] = ["","","","",""]
				self.search_3d_file(category, unpack_dir + "/" + obj_dir)
				self.iter = self.iter + 1
		rows_to_del = []
		for i in range(self.df.values.shape[0]):
			row = self.df.values[i]
			if row[1] == "":
				rows_to_del.append(i)
		self.df = self.df.drop(rows_to_del)
		#print(self.df.values)
		# store the table
		#self.df.to_pickle("./objects/objects.pkl")
		self.df.to_csv("./objects/objects.csv", sep=";")	
if __name__ == "__main__":
	s = Structurer()
	s.structure("./objects")