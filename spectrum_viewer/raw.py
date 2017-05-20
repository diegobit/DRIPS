import threading
import queue
import time
import os

import matplotlib.pyplot as plt
import matplotlib.animation as animation

filename = '/tmp/drips-data-monitor'

plotdataL = queue.Queue()
plotdataF = queue.Queue()
plotdataR = queue.Queue()

def update_xyvals(spectrum_message):
    strline = spectrum_message.strip()

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

    if strline[0] == 'l':
        plotdataL.put((xvals, yvals))
    elif strline[0] == 'f':
        plotdataF.put((xvals, yvals))
    elif strline[0] == 'r':
        plotdataR.put((xvals, yvals))

def retrievePlotData():
    while True:
        try:
            f = open(filename, encoding='utf8')
            f.seek(0, os.SEEK_END)
            f.readline() # Discard the first (possibly partial) line
            while True:
                line = f.readline()
                while len(line) == 0 or line[0] not in ['l', 'f', 'r']:
                    if not line:
                        time.sleep(0.1)
                    line = f.readline()

                update_xyvals(line)
        except OSError:
            print("Error reading file " + filename)
            time.sleep(1)

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
            plot.set_ylim([0, 1023])
        
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
    
    ani = animation.FuncAnimation(fig, animate, interval=100, blit=True)
    plt.show()
    os._exit(0)

threading.Thread(target=retrievePlotData).start()
main()