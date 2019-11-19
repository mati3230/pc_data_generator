import socket
from threading import Thread

class Server:
	def __init__(self, tcp_ip = "127.0.0.1", tcp_port = 5005, buffer_size = 20, message_handler=None):
		self.tcp_ip = tcp_ip
		self.tcp_port = tcp_port
		self.buffer_size = buffer_size
		self.message_handler = message_handler
		self.is_listening = False

		if not self.message_handler:
			raise Exception("message_handler is not assigned")

	def start_listen(self):
		print("start listening")
		self.is_listening = True
		self.listen()

	def listen(self):
		s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		s.bind((self.tcp_ip, self.tcp_port))
		s.listen(1)
		print("wait for client connection")
		conn, addr = s.accept()
		print("connection address: {0}".format(addr))
		while self.is_listening:
			data = conn.recv(self.buffer_size)
			if not data: 
				break
			if self.message_handler:		
				# the answer is expected as binary array, input is data as binary array
				answer = self.message_handler(data)
				conn.send(answer)
		conn.close()
		print("connection closed")

	def stop_listen(self):
		print("stop listening")
		self.is_listening = False
