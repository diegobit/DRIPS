/* 
 *  This code sends some test packets through the serial port
 *  to test the communication and the monitor
 */

/*********************************
 * ARDUINO-MONITOR MESSAGE FORMAT:
 *********************************
 *
 ********* INFO-MESSAGE:**********
 * Size: 25 Bytes
 * TABBBBBBBBCCCCCCCCDDDEFG\n
 *
 *     
 *      FIELD NAME        DIM
 * T    MessageType       1B
 * A    RoadID            1B
 * B    Manufacturer      8B
 * C    Model             8B
 * D    Orientation       3B
 * E    Priority          1B
 * F    RequestedAction   1B   // The action the car wants to do
 * G    CurrentAction     1B   // The action the car is doing to
 *                                cooperate with the network
 * 
 * FIELD              VALUE       DESCRIPTION
 * MessageType:       I           info message (this message)
 *                    L           frequency spectrum message from leftmost receiver
 *                    F           frequency spectrum message from front receiver
 *                    R           frequency spectrum message from rightmost receiver
 * RoadID:            M           my road
 *                    L           road to my left
 *                    A           road ahead
 *                    R           road to my right
 * Manufacturer:      Alfa⎵⎵⎵⎵
 *                    Fiat⎵⎵⎵⎵
 * Model:             Giulia⎵⎵
 *                    500⎵⎵⎵⎵⎵
 * Orientation:       [0..360]    degrees clockwise
 * Priority:          N           regular car
 *                    Y           priority car (ambulance, police)
 * RequestedAction,
 * CurrentAction:     S           Stay still
 *                    L           turn left
 *                    A           go straight ahead
 *                    R           turn right
 * 
 *** FREQUENCY-SPECTRUM-MESSAGE ***
 * Size: 257-769 Bytes (Assuming FFT_N = 128 = number of bins)
 * TA,A,...,A,A\n
 *  |_________|
 *      128    
 *     
 * A,A,...,A,A   The sensor data relative to the IR receiver
 * 
 *        FIELD NAME          DIM
 * T      MessageType         1B
 * A      BinFreqIntensity    1-5B
 * ,      BinsSeparator       1B    // separates each pair of bins
 * 
 * Each bin represents the intensity of a frequency range. The
 * frequencies go from 0 to sampling_frequency / 2. Bin i represents //TODO: check the end of the range
 * the range [i * sampling_frequency / 128, (i+1) * sampling_frequency / 128]
 * 
 * The intensity goes from 0 to 65535, it is sent as a decimal string, so it may be sent with a variable 
 * number of Bytes: from 1 Byte (eg. '6') up to 5 Bytes (eg. '65535')
 */

uint16_t bins1[64];
uint16_t bins2[64];
uint16_t bins3[64];

void setup() {
  Serial.begin(9600);

  for (uint16_t i = 0; i < 64; i++) {
    bins1[i] = i;
    bins2[i] = i;
    bins3[i] = i;
  }
}

void loop() {
  delay(2000); // 2 seconds
  
  // The car just arrived, it thinks it is alone (its sensors see nothing)
  Serial.write("IMAlfa    Giulia    0NLS\n");
  Serial.write('L');
  for(uint8_t i = 0; i < 63; i++) {
    Serial.print(bins1[i]);
    Serial.write(',');
  }
  Serial.print(bins1[63]);
  Serial.write('\n');

  Serial.write('F');
  for(uint8_t i = 0; i < 63; i++) {
    Serial.print(bins2[i]);
    Serial.write(',');
  }
  Serial.print(bins2[63]);
  Serial.write('\n');

  Serial.write('R');
  for(uint8_t i = 0; i < 63; i++) {
    Serial.print(bins3[i]);
    Serial.write(',');
  }
  Serial.print(bins3[63]);
  Serial.write('\n');

  delay(2000); // 2 seconds
  
  // The car sees 3 cars, it has not joined the network
  Serial.write("IMAlfa    Giulia    0NLS\n");
  Serial.write("IL                 90   \n");
  Serial.write("IA                180   \n");
  Serial.write("IR                270   \n");

  delay(2000); // 2 seconds
  
  // The car sees 2 cars and joined the network
  Serial.write("IMAlfa    Giulia    0NLS\n");
  Serial.write("ILFiat    500      90NAS\n");
  Serial.write("IRAlfa    Giulia  270NLL\n");
}
