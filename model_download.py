from selenium import webdriver
from selenium.webdriver.common.keys import Keys
from selenium.common.exceptions import NoSuchElementException
import time
import numpy as np
import requests
import os
#import traceback
import re

class Free3DDownloader:
	def __init__(self, open_browser=True):
		# download url
		self.d_url_part1 = "https://static.free3d.com/models"
		# search url		
		self.s_url_part1 = "https://free3d.com"
		if open_browser:
			firefox_profile = webdriver.FirefoxProfile()
			firefox_profile.set_preference("intl.accept_languages", "en-US, en")
			self.browser = webdriver.Firefox(firefox_profile=firefox_profile)
			#self.browser = webdriver.Firefox()
			# open browser with search url
			self.browser.get(self.s_url_part1)	

	# find all occurences of a substring in a string and return the indices 
	def find_allindices(self, string, sub, offset=0):
		listindex=[] 
		i = string.find(sub, offset)
		while i >= 0:
		    listindex.append(i)
		    i = string.find(sub, i + 1)
		return listindex

	def download_objs(self, category, source, objs_dir):
		# get all potential indices in the source where objects urls occur
		indexes = self.find_allindices(source, "data-seo_url")
		offset = 100
		indexes_offset = np.array(indexes) + offset
		# create directory if necessary
		path = objs_dir + "/" +  category
		if os.path.isdir(path) == False:
			os.makedirs(path)
		n_downloads = len(indexes)
		#for i in range(1):
		for i in range(n_downloads):
			print( "{:06.2f}%".format((i/n_downloads)*100) )
			# extract the url from the html source
			start_index = indexes[i]
			stop_index = indexes_offset[i]
			try:
				sub_address = source[start_index:stop_index]
			except:
				print("end of source")
				break
			fragments = sub_address.split("\"")
			# filter unneccessary items
			if fragments[1][:4] == "logs":
				continue
			# switch to obj page
			obj_name = fragments[1]
			try:
				self.browser.get(self.s_url_part1 + "/3d-model/"+obj_name+".html")
				# wait till page has loaded
				time.sleep(4)
			except:
				continue
			success, h_nr, filename = self.try_get_download_url()
			if success == False:
				print(obj_name + " is not available")
				continue
			# download file and save as f
			f = path + "/" + filename
			if(os.path.exists(f)):
				print(obj_name + " already downloaded")
				continue
			self.download_file(h_nr, filename, f)

	# download a file to a certain path given a url  
	def download_file(self, h_nr, filename, path):
		print(h_nr, filename, path)
		sub_urls = ["/2/"+h_nr+"/", "/dropbox/dropbox/sq/", "/", "/1/", "/2/", "/3/", "/4/", "/5/", "/6/", "/7/", "/s3/", "/123d/printable_catalog/"]
		
		for sub_url in sub_urls:		
			with requests.Session() as session:
				d_url = self.d_url_part1 + sub_url + filename
				print(d_url)
				r = session.get(d_url)
				if r.status_code == 200:
					with open(path, "wb") as bin_code:
						bin_code.write(r.content)
						print("done")
					break
			
	def download_category(self, category, objs_dir):
		# get search bar
		search = self.browser.find_element_by_css_selector("input[placeholder='search 3d models ...']")
		# type the category
		search.send_keys(category)
		# press enter
		search.send_keys(Keys.RETURN)
		# wait for the website
		time.sleep(4)
		# get the html source of the browser
		source = self.browser.page_source
		self.download_objs(category, source, objs_dir)
		self.browser.get(self.s_url_part1)

	# extract the download url and return if operation was successful
	def try_get_download_url(self):
		# possible file containers
		file_endings = ["fbx.rar", ".rar", ".zip"]
		try:
			# find download button
			button = self.browser.find_element_by_link_text("Download")
			button.click()
		except:
			print("No download button available")
			return False, "", ""
		# search download link for every file ending
		for ending in file_endings:
			try:
				button = self.browser.find_element_by_partial_link_text(ending)
				btn_href = button.get_attribute("href")
				# text of the download link
				d_name = button.text
				# filter the text so that only filename exists
				fragments = re.split(" |\n", d_name)
				#print(fragments) 
				d_name = fragments[0]
				# check if filtered filename is valid
				if d_name[-len(ending):] != ending:
					print("Name problem: " + d_name + ", ending: " + ending)
					continue
				# https://free3d.com/dl-files.php?p=5cb58ec626be8bcb7a8b4567&f=2
				# nr that is used in download url
				h_nr = btn_href.split("=")[1]
			except:
				#traceback.print_exc()
				continue
			# return success, download url, name of the object
			return True, h_nr[:-2], d_name
		return False, "", ""

	def quit(self):
		self.browser.quit()

if __name__ == "__main__":
	downloader = Free3DDownloader(open_browser=True)
	#downloader.download_file("https://static.free3d.com/models/2/5439e5c226be8bbb4c8b4567/1f9jtr180dxk-Tree1ByTyroSmith.zip", "./objects/test.zip")
	#downloader.download_file("https://static.free3d.com/models/2/5cb58ec626be8bcb7a8b4567/28-01alocasia_fbx.rar", "./objects/test.zip")
	#https://static.free3d.com/models/dropbox/dropbox/sq/tree.zip
	#https://static.free3d.com/models/dfjau1mq78qo-trees_lo-poly.zip
	#https://static.free3d.com/models/1/cim59vrioikg-IndoorPotPlant.zip
	#https://static.free3d.com/models/whnp3v2jflkw-Tree.rar
	#https://static.free3d.com/models/s3/81ai2k8rqg-Date_palm.rar
	# https://static.free3d.com/models/123d/printable_catalog/10450_Rectangular_Grass_Patch_L3.123c827d110a-1347-4381-9208-e4f735762647.zip
	# 
	objs_dir = "./objects"
	downloader.download_category("Plants", objs_dir)
	downloader.download_category("Cars", objs_dir)

	downloader.quit()
