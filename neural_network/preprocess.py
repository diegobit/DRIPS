import pickle
import numpy as np

# Frequencies associated to each position of the car
# (it's actually a bin number, not a Hz frequency, so they go from 0 to 63. But don't use 0 and 63)
fleft = 10
fleftfront = 20
ffront = 30
frightfront = 40
fright = 50

inputFile = './dataset.csv'
outputFile = './data.pickle'


# 44 23 71 9  88
# 3  1  0  2  4
def ranksi(array):
    return [x/(len(array)-1) for x in np.array(array).argsort().tolist()] # Normalize data in [0, 1]


content = ''
data = []

# Load file
with open(inputFile, 'r') as content_file:
    content = content_file.read()


# Convert file
lines = content.split('\n')
for i in range(0, len(lines)-1, 3):
    lineL = lines[i]
    lineF = lines[i+1]
    lineR = lines[i+2]
    if (len(lineL) == 0 or len(lineF) == 0 or len(lineR) == 0):
        continue
    data.append([ [int(x) for x in lineL.split(',')], [int(x) for x in lineF.split(',')], [int(x) for x in lineR.split(',')] ])


# Preprocess data
out = []
for i in range(len(data)):
    fftL = data[i][0]
    fftF = data[i][1]
    fftR = data[i][2]

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

    # Normalize in [0, 1]
    maxval = max(max(fftL), max(fftF), max(fftR))
    fftL = [x / maxval for x in fftL]
    fftF = [x / maxval for x in fftF]
    fftR = [x / maxval for x in fftR]

    out.append(fftL + fftF + fftR + ranksi(fftL) + ranksi(fftF) + ranksi(fftR))

# Write data
with open(outputFile, 'wb') as f:
    pickle.dump(out, f)