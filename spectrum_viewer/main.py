import serial
import threading
import queue

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

plotdataL = queue.Queue()
plotdataF = queue.Queue()
plotdataR = queue.Queue()


def retrievePlotData():
    ser = serial.Serial('/dev/ttyACM0', 230400)
    ser.readline() # Discard the first (possibly partial) line
    while True:
        print("Syncing...")
        line = b''
        while len(line) == 0 or chr(line[0]) not in ['L', 'F', 'R']:
            line = ser.readline()

        yvals = line.decode('utf-8').strip()[2:].split(',')
        yvals = [float(v) for v in yvals]

        # Remove low frequencies
        yvals[0] = 0
        yvals[1] = 0
        yvals[2] = 0

        xvals = [i for i in range(len(yvals))]
        if chr(line[0]) == 'L':
            plotdataL.put((xvals, yvals))
        elif chr(line[0]) == 'F':
            plotdataF.put((xvals, yvals))
        elif chr(line[0]) == 'R':
            plotdataR.put((xvals, yvals))

def main():
    def animate(i):
        try:
            x, y = plotdataL.get(block=False, timeout=None)
            plotL.cla()
            plotL.bar(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataF.get(block=False, timeout=None)
            plotF.cla()
            plotF.bar(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataR.get(block=False, timeout=None)
            plotR.cla()
            plotR.bar(x, y)
        except queue.Empty:
            pass

    fig = plt.figure()
    plotL = fig.add_subplot(131)
    plotF = fig.add_subplot(132)
    plotR = fig.add_subplot(133)
    ani = animation.FuncAnimation(fig, animate, interval=100)
    plt.show()

threading.Thread(target=retrievePlotData).start()
threading.Thread(target=main).start()