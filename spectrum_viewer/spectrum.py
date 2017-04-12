import serial
import threading
import queue
import time
import random
import os

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
    try:
        yvals = [float(v) for v in yvals]
    except:
        print(yvals)
        return

    # Remove low frequencies
    #yvals[0] = 0
    #yvals[1] = 0
    #yvals[2] = 0

    FFT_N = 2 * len(yvals)
    # ((i*Fs/N) + ((i+1)*Fs/N)) / 2
    xvals = [((i*Fs/FFT_N) + ((i+1)*Fs/FFT_N)) / 2.0 for i in range(len(yvals))]

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
            while not plotdataL.empty():
                plotdataL.get()
            plotL.lines[0].set_data(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataF.get(block=False, timeout=None)
            while not plotdataF.empty():
                plotdataF.get()
            plotF.lines[0].set_data(x, y)
        except queue.Empty:
            pass

        try:
            x, y = plotdataR.get(block=False, timeout=None)
            while not plotdataR.empty():
                plotdataR.get()
            plotR.lines[0].set_data(x, y)
        except queue.Empty:
            pass
        
        plotLFR.lines[0].set_data(plotL.lines[0].get_xdata(), plotL.lines[0].get_ydata())
        plotLFR.lines[1].set_data(plotF.lines[0].get_xdata(), plotF.lines[0].get_ydata())
        plotLFR.lines[2].set_data(plotR.lines[0].get_xdata(), plotR.lines[0].get_ydata())
        
        for plot in [plotL, plotF, plotR, plotLFR]:
            if len(plot.lines) > 0 and len(plot.lines[0].get_xdata()) > 0:
                plot.set_xlim([0, plot.lines[0].get_xdata()[-1]])
            plot.set_ylim([0, 200])
        
        return plotL.lines[0], plotF.lines[0], plotR.lines[0], plotLFR.lines[0], plotLFR.lines[1], plotLFR.lines[2]

    fig = plt.figure()
    
    plotL = plt.subplot2grid((5, 3), (3, 0), rowspan=2)
    plotF = plt.subplot2grid((5, 3), (3, 1), rowspan=2)
    plotR = plt.subplot2grid((5, 3), (3, 2), rowspan=2)
    plotLFR = plt.subplot2grid((5, 3), (0, 0), colspan=3, rowspan=3)
    
    plotL.plot([], [], 'C0')
    plotF.plot([], [], 'C1')
    plotR.plot([], [], 'C2')
    plotLFR.plot([], [], 'C0', [], [], 'C1', [], [], 'C2')
    
    plotL.set_title("Left sensor")
    plotF.set_title("Front sensor")
    plotR.set_title("Right sensor")
    plotLFR.set_title("All sensors")

    for plot in [plotL, plotF, plotR, plotLFR]:
        plot.set_xlabel("Frequency (Hz)")
        plot.set_ylabel("Intensity")
    
    plt.tight_layout()
    fig.subplots_adjust(hspace=1.5)
    
    ani = animation.FuncAnimation(fig, animate, interval=100)
    plt.show()
    os._exit(0)

threading.Thread(target=retrievePlotData).start()
threading.Thread(target=main).start()
