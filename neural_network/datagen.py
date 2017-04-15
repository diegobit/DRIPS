import random
import pickle
import numpy as np

# Under this level (included), a bin is considered noise
maxnoise = 10

def getRandomSensorData(peaks):
    # background noise
    v = [random.randint(0, maxnoise) for i in range(64)]
    for i in range(len(peaks)):
        peak = peaks[i]
        maxval = maxnoise + (len(peaks) - i) * (130 - maxnoise) / len(peaks)
        minval = maxval - 20
        v[peak] = random.randint(minval, maxval)
        if peak > 0:
            v[peak-1] = random.randint(maxnoise, minval)
        if peak < 63:
            v[peak+1] = random.randint(maxnoise, minval)
    return v


def getRandomData(peaksL, peaksF, peaksR):
    return [getRandomSensorData(peaksL), getRandomSensorData(peaksF), getRandomSensorData(peaksR)]


# Frequencies associated to each position of the car
# (it's actually a bin number, not a Hz frequency, so they go from 0 to 63. But don't use 0 and 63)
fleft = 10
fleftfront = 20
ffront = 30
frightfront = 40
fright = 50

# Acquire raw data

out = []

# left
for i in range(10000):
    out.append([
        getRandomData([fleft], [], []),
        [0, 0, 0, 0, 1, 0, 0, 0]
    ])

# front
for i in range(10000):
    out.append([
        getRandomData([], [ffront], []),
        [0, 0, 1, 0, 0, 0, 0, 0]
    ])

# right
for i in range(10000):
    out.append([
        getRandomData([], [], [fright]),
        [0, 1, 0, 0, 0, 0, 0, 0]
    ])

# Preprocess data

n_peaks = 6
for i in range(len(out)):
    fftL = out[i][0][0]
    fftF = out[i][0][1]
    fftR = out[i][0][2]

    # Sharpen peaks.
    # For each known frequency f, set
    #       v(f) = max(v(f-1), v(f), v(f+1));
    #       v(f-1) = f(f+1) = 0;
    # (here f is actually a bin number)
    for f in [fleft, fleftfront, ffront, frightfront, fright]:
        fftL[f] = max(fftL[f-1], fftL[f], fftL[f+1])
        fftF[f] = max(fftF[f-1], fftF[f], fftF[f+1])
        fftR[f] = max(fftR[f-1], fftR[f], fftR[f+1])
        fftL[f-1] = fftL[f+1] = 0
        fftF[f-1] = fftF[f+1] = 0
        fftR[f-1] = fftR[f+1] = 0

    # get max peak indices:
    peaksL = np.argsort(fftL)[::-1][:n_peaks].tolist()
    peaksF = np.argsort(fftF)[::-1][:n_peaks].tolist()
    peaksR = np.argsort(fftR)[::-1][:n_peaks].tolist()

    # set noise peaks to zero:
    peaksL = [0 if fftL[peaksL[i]] <= maxnoise else peaksL[i] for i in range(len(peaksL))]
    peaksF = [0 if fftF[peaksF[i]] <= maxnoise else peaksF[i] for i in range(len(peaksF))]
    peaksR = [0 if fftR[peaksR[i]] <= maxnoise else peaksR[i] for i in range(len(peaksR))]

    out[i][0] = peaksL + peaksF + peaksR



print(out[0])
print(out[1])
print(out[-1])

with open('data.pickle', 'wb') as f:
    pickle.dump(out, f)

print("Saved dataset. Length: " + str(len(out)))

# labels:
#
#   000 -> 1,0,0,0,0,0,0,0  (no cars)
#   001 -> 0,1,0,0,0,0,0,0  (just one car on the right)
#   010 -> 0,0,1,0,0,0,0,0  (just one car in front of us)
#   011 -> 0,0,0,1,0,0,0,0  (one car in front of us, and one on the right)
#   100 -> 0,0,0,0,1,0,0,0  (just one car on the left)
#   101 -> 0,0,0,0,0,1,0,0  ...
#   110 -> 0,0,0,0,0,0,1,0  ...
#   111 -> 0,0,0,0,0,0,0,1  ...