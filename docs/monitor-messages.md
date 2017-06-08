# ARDUINO-MONITOR MESSAGE FORMAT

## COMMON MESSAGE FORMAT

Size: >= 1 Byte

    TP...P\n

|   | FIELD NAME       | DIM |
|---|------------------|-----|
| T | MessageType      | 1 B |
| P | Payload          | any |

This is the format which is used by all the messages.

### Field: MessageType

| Value | Description                                        |
|-------|----------------------------------------------------|
| `I`   | info message                                       |
| `l`   | sampled data from leftmost receiver                |
| `f`   | sampled data from front receiver                   |
| `r`   | sampled data from rightmost receiver               |
| `L`   | frequency spectrum message from leftmost receiver  |
| `F`   | frequency spectrum message from front receiver     |
| `R`   | frequency spectrum message from rightmost receiver |

### Field: Payload

| Value | Description                                                          |
|-------|----------------------------------------------------------------------|
| any   | Data specific to the current message type (see detailed specs below) |


## INFO-MESSAGE

Size: 26 Bytes

    TABCCCCCCCCDDDDDDDDEEEFGH\n

|   | FIELD NAME       | DIM | COMMENT                                                   |
|---|------------------|-----|-----------------------------------------------------------|
| T | MessageType      | 1 B |                                                           |
| A | RoadID           | 1 B |                                                           |
| B | IsEmpty          | 1 B | Whether the RoadID side of the road is empty              |
| C | Manufacturer     | 8 B |                                                           |
| D | Model            | 8 B |                                                           |
| E | Orientation      | 3 B |                                                           |
| F | Priority         | 1 B |                                                           |
| G | RequestedAction  | 1 B | The action the car wants to do                            |
| H | CurrentAction    | 1 B | The action the car is doing to cooperate with the network |

### Field: MessageType

| Value | Description  |
|-------|--------------|
| `I`   | info message |

### Field: RoadID

| Value | Description      |
|-------|------------------|
| `M`   | my road          |
| `L`   | road to my left  |
| `A`   | road ahead       |
| `R`   | road to my right |

### Field: IsEmpty

| Value | Description |
|-------|-------------|
| `0`   | false       |
| `1`   | true        |

### Field: Manufacturer

| Value          | Description                                                      | Example    |
|----------------|------------------------------------------------------------------|------------|
| 8 chars string | 8 bytes string without terminator, with space padding at the end | `Tesla⎵⎵⎵` |
| 8 spaces       | Sent when manufacturer is unknown                                | `⎵⎵⎵⎵⎵⎵⎵` |

### Field: Model

| Value          | Description                                                      | Example    |
|----------------|------------------------------------------------------------------|------------|
| 8 chars string | 8 bytes string without terminator, with space padding at the end | `Model S⎵` |
| 8 spaces       | Sent when model is unknown                                       | `⎵⎵⎵⎵⎵⎵⎵` |

### Field: Orientation

| Value    | Description                                               |
|----------|-----------------------------------------------------------|
| [0..360] | Counterclockwise degrees. The number is sent as a string. |

### Field: Priority

| Value | Description |
|-------|-------------|
| `0`   | Unspecified |
| `N`   | No priority |
| `Y`   | Priority    |

### Field: RequestedAction

| Value | Description       |
|-------|-------------------|
| `0`   | None              |
| `L`   | Turn left         |
| `A`   | Go straight ahead |
| `R`   | Turn right        |

### Field: CurrentAction

| Value | Description       |
|-------|-------------------|
| `0`   | None              |
| `S`   | Stay still        |
| `L`   | Turn left         |
| `A`   | Go straight ahead |
| `R`   | Turn right        |


## SAMPLED-DATA-MESSAGE

Size: 259-652 Bytes (Assuming FFT_N = 128 = number of samples)

    TM;A,A,...,A,A\n
       |_________|
           128

`A,A,...,A,A` is the sensor data relative to the IR receiver

|   | FIELD NAME      | DIM    | Notes                                                   |
|---|-----------------|--------|---------------------------------------------------------|
| T | MessageType     | 1 B    | one of `l`, `f`, `r`                                    |
| M | SamplingPeriod  | 1-10 B | (μs) the sampling period used in the X-axis of the plot |
| A | SampleValue     | 1-4 B  | values go from 0 to 1023                                |
| , | SampleSeparator | 1 B    | separates each pair of samples                          |
| ; | HeaderSeparator | 1 B    | separates the header from the data                      |


## FREQUENCY-SPECTRUM-MESSAGE

Size: 131-396 Bytes (Assuming FFT_N = 128 = 2 * number of bins)

    TM;A,A,...,A,A\n
       |_________|
           64

`A,A,...,A,A` is the sensor data relative to the IR receiver

|   | FIELD NAME       | DIM    | Notes                                                   |
|---|------------------|--------|---------------------------------------------------------|
| T | MessageType      | 1 B    | one of `L`, `F`, `R`                                    |
| M | SamplingPeriod   | 1-10 B | (μs) the sampling period used in the X-axis of the plot |
| A | BinFreqIntensity | 1-5 B  |                                                         |
| , | BinsSeparator    | 1 B    | separates each pair of bins                             |
| ; | HeaderSeparator  | 1 B    | separates the header from the data                      |

Each bin represents the intensity of a frequency range. The frequencies go from 0 to samplingFrequency / 2 (where samplingFrequency = 1 / samplingPeriod * 1000000).
Bin i represents the range [i * sampling_frequency / 128, (i+1) * sampling_frequency / 128].
