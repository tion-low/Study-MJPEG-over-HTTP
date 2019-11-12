mjpeg-server:
	mjpeg-server -- ffmpeg \
	-loglevel error \
	-probesize 32 \
	-fpsprobesize 0 \
	-analyzeduration 0 \
	-fflags nobuffer \
	-f avfoundation \
	-capture_cursor 1 \
	-r 15 \
	-pixel_format yuyv422 \
	-i 1 \
	-f mpjpeg \
	-q 2 \
	-
