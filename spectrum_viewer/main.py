import serial

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

ser = serial.Serial('/dev/ttyACM3', 9600)

def getFFTData():
    print("Syncing...")
    line = ""
    while line != b'--begin-fft--\r\n':
        line = ser.readline()
    
    print("Read start")
    xvals = []
    yvals = []
    while line != b'--end-fft--\r\n':
        line = ser.readline()
        if line != b'--end-fft--\r\n':
            strdata = line.decode("utf-8").strip()
            x1x2, y = strdata.split(" = ")
            x1, x2 = x1x2.split(" -> ")
            x = (float(x1) + float(x2)) / 2.0
            
            xvals.append(x)
            yvals.append(float(y))
    print("Read end")
    
    # Remove low frequencies
    yvals[0] = 0
    yvals[1] = 0
    yvals[2] = 0
    return xvals, yvals
    


def animate(i):
    x, y = getFFTData();
    ax.cla()
    ax.bar(x, y)

fig, ax = plt.subplots()

rects = ax.bar([], [])

ani = animation.FuncAnimation(fig, animate)
plt.show()