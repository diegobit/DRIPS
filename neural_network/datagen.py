import random
import pickle


def getRandomSensorData(peaks):
    v = [random.randint(0, 10) for i in range(64)]
    for peak in peaks:
        v[peak] = random.randint(100, 130)
        if peak > 0:
            v[peak-1] = random.randint(10, 100)
        if peak < 63:
            v[peak+1] = random.randint(10, 100)
    return v


def getRandomData(peaksL, peaksF, peaksR):
    return getRandomSensorData(peaksL) + getRandomSensorData(peaksF) + getRandomSensorData(peaksR)

out = []

# left 40
for i in range(10000):
    out.append([
        getRandomData([40], [], []),
        [0, 0, 0, 0, 1, 0, 0, 0]
    ])

# front 40
for i in range(10000):
    out.append([
        getRandomData([], [40], []),
        [0, 0, 1, 0, 0, 0, 0, 0]
    ])

# right 40
for i in range(10000):
    out.append([
        getRandomData([], [], [40]),
        [0, 1, 0, 0, 0, 0, 0, 0]
    ])



with open('data.pickle', 'wb') as f:
    pickle.dump(out, f)

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