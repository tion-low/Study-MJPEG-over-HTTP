DEVICE?=1
PORT?=9000

mjpeg-server:
	mjpeg-server -a :${PORT} -- ffmpeg \
	-loglevel error \
	-probesize 32 \
	-fpsprobesize 0 \
	-analyzeduration 0 \
	-fflags nobuffer \
	-f avfoundation \
	-capture_cursor 1 \
	-r 15 \
	-pixel_format yuyv422 \
	-i ${DEVICE} \
	-f mpjpeg \
	-q 2 \
	-

show-devices:
	ffmpeg -hide_banner -f avfoundation -list_devices true -i ""
