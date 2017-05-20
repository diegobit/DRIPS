import pickle
import numpy as np

def periodToBin(t):
    FHT_N = 128
    SAMPLING_PERIOD = 470
    TIMER_PERIOD = 100
    return int((FHT_N * SAMPLING_PERIOD) / (t * TIMER_PERIOD))

# Frequencies associated to each position of the car
# (it's actually a bin number, not a Hz frequency, so they go from 0 to 63. But don't use 0 and 63)
fleft = periodToBin(160) # FIXME Is this actually the left led?
fleftfront = periodToBin(40)
ffront = periodToBin(24)
frightfront = periodToBin(16)
fright = periodToBin(12)
print(fleft)
print(fleftfront)
print(ffront)
print(frightfront)
print(fright)


outputFile = './data.pickle'


# 44 23 71 9  88
# 3  1  0  2  4
def ranksi(array):
    return [x/(len(array)-1) for x in np.array(array).argsort().tolist()] # Normalize data in [0, 1]

def network_output(crossroad_config):
    if crossroad_config == '000':
        return [1, 0, 0, 0, 0, 0, 0, 0]
    elif crossroad_config == '001':
        return [0, 1, 0, 0, 0, 0, 0, 0]
    elif crossroad_config == '010':
        return [0, 0, 1, 0, 0, 0, 0, 0]
    elif crossroad_config == '011':
        return [0, 0, 0, 1, 0, 0, 0, 0]
    elif crossroad_config == '100':
        return [0, 0, 0, 0, 1, 0, 0, 0]
    elif crossroad_config == '101':
        return [0, 0, 0, 0, 0, 1, 0, 0]
    elif crossroad_config == '110':
        return [0, 0, 0, 0, 0, 0, 1, 0]
    elif crossroad_config == '111':
        return [0, 0, 0, 0, 0, 0, 0, 1]
    else:
        raise Exception('Unknown crossroad config: ' + crossroad_config)


out = []

for crossroad_config in ['000', '001', '010', '011', '100', '101', '110', '111']:

    # Load file
    content = ''
    with open("dataset_" + crossroad_config + ".csv", 'r') as content_file:
        content = content_file.read()

    # Convert file
    data = []
    lines = content.split('\n')
    for i in range(0, len(lines)-2, 3):
        lineL = lines[i]
        lineF = lines[i+1]
        lineR = lines[i+2]
        if (len(lineL) == 0 or len(lineF) == 0 or len(lineR) == 0):
            continue
        data.append([ [int(x) for x in lineL.split(',')], [int(x) for x in lineF.split(',')], [int(x) for x in lineR.split(',')] ])

    # Preprocess data
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

        out.append([
            fftL + fftF + fftR + ranksi(fftL) + ranksi(fftF) + ranksi(fftR),
            network_output(crossroad_config)
        ])

# Write data
with open(outputFile, 'wb') as f:
    pickle.dump(out, f)