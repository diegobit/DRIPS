import random
import pickle
import numpy as np

# Under this level (included), a bin is considered noise
maxnoise = 10

# 44 23 71 9  88
# 2  1  3  0  4
def ranks(array):
    array = np.array(array)
    temp = array.argsort()
    ranks = np.empty(len(array), int)
    ranks[temp] = np.arange(len(array))
    return ranks.tolist()

# 44 23 71 9  88
# 3  1  0  2  4
def ranksi(array):
    return [x/len(array) for x in np.array(array).argsort().tolist()] # Normalize data in [0, 1]

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
    return [x/1024 for x in v] # Normalize data in [0, 1]


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

# 000
for i in range(10000):
    out.append([
        getRandomData([], [], []),
        [1, 0, 0, 0, 0, 0, 0, 0]
    ])

# 100
for i in range(10000):
    out.append([
        getRandomData([fright], [], []),
        [0, 0, 0, 0, 1, 0, 0, 0]
    ])

# 010
for i in range(10000):
    out.append([
        getRandomData([], [ffront], []),
        [0, 0, 1, 0, 0, 0, 0, 0]
    ])

# 001
for i in range(10000):
    out.append([
        getRandomData([], [], [fleft]),
        [0, 1, 0, 0, 0, 0, 0, 0]
    ])

# 011
for i in range(10000):
    out.append([
        getRandomData([], [ffront], [fleft]),
        [0, 0, 0, 1, 0, 0, 0, 0]
    ])

# 110
for i in range(10000):
    out.append([
        getRandomData([fright], [ffront], []),
        [0, 0, 0, 0, 0, 0, 1, 0]
    ])

# 101
for i in range(10000):
    out.append([
        getRandomData([fright], [], [fleft]),
        [0, 0, 0, 0, 0, 1, 0, 0]
    ])

# 111
for i in range(10000):
    out.append([
        getRandomData([fright], [ffront], [fleft]),
        [0, 0, 0, 0, 0, 0, 0, 1]
    ])

# Preprocess data

n_peaks = 5
for i in range(len(out)):
    fftL = out[i][0][0]
    fftF = out[i][0][1]
    fftR = out[i][0][2]

    # Sharpen peaks.
    # For each known frequency f, set
    #       v(f) := max(v(f-1), v(f), v(f+1))
    # (here f is actually a bin number)
    for f in [fleft, fleftfront, ffront, frightfront, fright]:
        fftL[f] = max(fftL[f-1], fftL[f], fftL[f+1])
        fftF[f] = max(fftF[f-1], fftF[f], fftF[f+1])
        fftR[f] = max(fftR[f-1], fftR[f], fftR[f+1])

    # Take only the meaningful frequencies
    fftL = [fftL[fleft], fftL[fleftfront], fftL[ffront], fftL[frightfront], fftL[fright]]
    fftF = [fftF[fleft], fftF[fleftfront], fftF[ffront], fftF[frightfront], fftF[fright]]
    fftR = [fftR[fleft], fftR[fleftfront], fftR[ffront], fftR[frightfront], fftR[fright]]

    out[i][0] = fftL + fftF + fftR + ranksi(fftL) + ranksi(fftF) + ranksi(fftR)



print(out[0])
print(out[1])
print(out[-2])
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