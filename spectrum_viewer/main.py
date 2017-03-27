import serial
import threading
import queue
import time
import random

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

plotdataL = queue.Queue()
plotdataF = queue.Queue()
plotdataR = queue.Queue()

def update_xyvals(spectrum_message):
    strline = spectrum_message.decode('utf-8').strip()

    # Sampling freq
    Fs = 1 / (float(strline[1:].split(';')[0]) * pow(10, -6))

    yvals = strline[1:].split(';')[1].split(',')
    yvals = [float(v) for v in yvals]

    # Remove low frequencies
    yvals[0] = 0
    yvals[1] = 0
    yvals[2] = 0

    N = len(yvals)
    # ((i*Fs/N) + ((i+1)*Fs/N)) / 2
    xvals = [((i*Fs/N) + ((i+1)*Fs/N)) / 2.0 for i in range(N)]

    if strline[0] == 'L':
        plotdataL.put((xvals, yvals))
    elif strline[0] == 'F':
        plotdataF.put((xvals, yvals))
    elif strline[0] == 'R':
        plotdataR.put((xvals, yvals))

def retrievePlotData():
    ser = serial.Serial('/dev/ttyACM0', 230400)
    ser.readline() # Discard the first (possibly partial) line
    while True:
        line = b''
        while len(line) == 0 or chr(line[0]) not in ['L', 'F', 'R']:
            print("Syncing...")
            line = ser.readline()
        update_xyvals(line)
        
        # Dummy data generator (comment the above lines and uncomment these to enable it)
        #time.sleep(0.1)
        #line = b'L200;' + ','.join([str((200 if i == 9 else 700 if i == 10 else 300 if i == 11 else i) + random.randint(0, 10)) for i in range(128)]).encode('utf-8')
        #update_xyvals(line)
        #line = b'F200;' + ','.join([str((250 if i == 20 else 800 if i == 21 else 400 if i == 22 else i) + random.randint(0, 10)) for i in range(128)]).encode('utf-8')
        #update_xyvals(line)
        #line = b'R200;' + ','.join([str((700 if i == 30 else 900 if i == 31 else 650 if i == 32 else i) + random.randint(0, 10)) for i in range(128)]).encode('utf-8')
        #update_xyvals(line)

def main():
    def animate(i):
        try:
            x, y = plotdataL.get(block=False, timeout=None)
            plotL.cla()
            plotL.plot(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataF.get(block=False, timeout=None)
            plotF.cla()
            plotF.plot(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataR.get(block=False, timeout=None)
            plotR.cla()
            plotR.plot(x, y)
        except queue.Empty:
            pass
        
        plotL.set_title("Left sensor")
        plotF.set_title("Front sensor")
        plotR.set_title("Right sensor")
        plotL.set_xlabel("Frequency")
        plotL.set_ylabel("Intensity")
        plotF.set_xlabel("Frequency")
        plotF.set_ylabel("Intensity")
        plotR.set_xlabel("Frequency")
        plotR.set_ylabel("Intensity")

    fig = plt.figure()
    plotL = fig.add_subplot(223)
    plotF = fig.add_subplot(221)
    plotR = fig.add_subplot(224)
    plt.tight_layout()
    ani = animation.FuncAnimation(fig, animate, interval=100)
    plt.show()

threading.Thread(target=retrievePlotData).start()
threading.Thread(target=main).start()