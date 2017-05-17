import serial
import json

serialPort = '/dev/tty.usbmodem1441'
baud = 230400


datalen = 0
buffer = []
crossroad_config = ''

def writeBuffer():
    global buffer
    with open("dataset_" + crossroad_config + ".csv", "a") as myfile:
        myfile.write(','.join(buffer[0]) + "\n")
        myfile.write(','.join(buffer[1]) + "\n")
        myfile.write(','.join(buffer[2]) + "\n")

def incomingLine(line):
    global buffer, datalen
    strline = line.decode('utf-8').strip()

    sensor = strline[0]

    yvals = strline[1:].split(';')[1].split(',')
    if len(yvals) != 64:
        return
    
    if sensor == 'L' and len(buffer) == 0:
        buffer.append(yvals)
    elif sensor == 'F' and len(buffer) == 1:
        buffer.append(yvals)
    elif sensor == 'R' and len(buffer) == 2:
        buffer.append(yvals)
        writeBuffer()
        datalen = datalen + 1
        print("Dataset size: " + str(datalen))
        buffer = []
    else:
        buffer = []

crossroad_config = input("Configurazione incrocio (000, 001, 010, ..., 111): ")
print("dataset_" + crossroad_config + ".csv")

print("Connecting to " + serialPort + "...")
ser = serial.Serial(serialPort, baud)

ser.readline() # Discard the first (possibly partial) line
while True:
    line = b''
    while len(line) == 0 or chr(line[0]) not in ['L', 'F', 'R']:
        line = ser.readline()
    incomingLine(line)