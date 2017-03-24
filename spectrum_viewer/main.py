import serial

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

ser = serial.Serial('/dev/ttyACM3', 9600)

def getFFTData():
    print("Syncing...")
    line = ""
    while line[0] != b'R':
        line = ser.readline()
    
    yvals = line.decode("utf-8").strip()[1:].split(',')
    yvals = [float(v) for v in yvals]
    
    # Remove low frequencies
    yvals[0] = 0
    yvals[1] = 0
    yvals[2] = 0

    xvals = [i for i in range(len(yvals))]
    return xvals, yvals
    


def animate(i):
    x, y = getFFTData()
    ax.cla()
    ax.bar(x, y)

fig, ax = plt.subplots()

rects = ax.bar([], [])

ani = animation.FuncAnimation(fig, animate)
plt.show()