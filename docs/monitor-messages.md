# ARDUINO-MONITOR MESSAGE FORMAT:


## INFO-MESSAGE

Size: 25 Bytes  
TABBBBBBBBCCCCCCCCDDDEFG\n

|   |  FIELD NAME       | DIM |
|---|-------------------|-----|
| T |  MessageType      | 1B  |
| A |  RoadID           | 1B  |
| B |  Manufacturer     | 8B  |
| C |  Model            | 8B  |
| D |  Orientation      | 3B  |
| E |  Priority         | 1B  |
| F |  RequestedAction  | 1B  | // The action the car wants to do
| G |  CurrentAction    | 1B  | // The action the car is doing to cooperate with the network

 FIELD              VALUE       DESCRIPTION  
 MessageType:       I           info message (this message)  
                    l           sampled data from leftmost receiver (for debug)
                    f           sampled data from front receiver (for debug)
                    r           sampled data from rightmost receiver (for debug)
                    L           frequency spectrum message from leftmost receiver  
                    F           frequency spectrum message from front receiver  
                    R           frequency spectrum message from rightmost receiver

 RoadID:            M           my road  
                    L           road to my left  
                    A           road ahead  
                    R           road to my right

 Manufacturer:      Alfa⎵⎵⎵⎵  
                    Fiat⎵⎵⎵⎵  
                    Tesla⎵⎵⎵

 Model:             Giulia⎵⎵  
                    500⎵⎵⎵⎵⎵  
                    Model S⎵

 Orientation:       [0..360]    degrees clockwise

 Priority:          N           regular car  
                    Y           priority car (ambulance, police)

 RequestedAction,
 CurrentAction:     S           Stay still  
                    L           turn left  
                    A           go straight ahead  
                    R           turn right


## SAMPLED-DATA-MESSAGE

Size: 259-652 Bytes (Assuming FFT_N = 128 = number of samples)
TM;A,A,...,A,A\n
   |_________|
       128

A,A,...,A,A     is the sensor data relative to the IR receiver

       FIELD NAME          DIM      Notes
T      MessageType         1B       one of `l`, `f`, `r`
M      SamplingPeriod      1-10B    (us) the sampling period used in the X-axis of the plot
A      SampleValue         1-4B     values are from 0 to 1023
,      SampleSeparator     1B       separates each pair of samples
;      headerSeparator     1B       separates the header from the data


## FREQUENCY-SPECTRUM-MESSAGE

Size: 131-396 Bytes (Assuming FFT_N = 128 = 2 * number of bins)  
TM;A,A,...,A,A\n
   |_________|
       64

A,A,...,A,A     is the sensor data relative to the IR receiver

       FIELD NAME          DIM      Notes  
T      MessageType         1B       one of `L`, `F`, `R`
M      SamplingPeriod      1-10B    (us) the sampling period used in the X-axis of the plot  
A      BinFreqIntensity    1-5B  
,      BinsSeparator       1B       separates each pair of bins  
;      headerSeparator     1B       separates the header from the data

Each bin represents the intensity of a frequency range. The frequencies go from 0 to samplingFrequency / 2 (where samplingFrequency = 1 / samplingPeriod * 1000000).  
Bin i represents the range [i * sampling_frequency / 128, (i+1) * sampling_frequency / 128].