/* 
 *  This code sends some test packets through the serial port
 *  to test the communication and the monitor
 */

/*********************************
 * ARDUINO-MONITOR MESSAGE FORMAT:
 *********************************
 *
 ********* INFO-MESSAGE:**********
 * Size: 24 Bytes
 * ABBBBBBBBCCCCCCCCDDDEFG\n
 *
 *     
 *      FIELD NAME        DIM
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
 * Road_ID:           M           my road
 *                    L           road to my left
 *                    A           road ahead
 *                    R           road to my right
 * Manufacturer:      ⎵⎵⎵⎵Alfa
 *                    ⎵⎵⎵⎵Fiat
 * Model:             ⎵⎵Giulia
 *                    ⎵⎵⎵⎵⎵500
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
 * Size: 258 Bytes (Assuming FFT_N = 128 = number of bins)
 * ABBB...BBB\n
 *  |_______|
 *     256
 *     
 *     FIELD NAME         DIM
 * A   RoadID             1B
 * B   BinFreqIntensity   2B
 * 
 * Each bin represents the intensity of a frequency range. The
 * frequencies go from 0 to SAMPLING_FREQ. Bin i represents
 * the range [i * SAMPLING_FREQ / 128, (i+1) * SAMPLING_FREQ / 128]
 * 
 * The intensity (2 Bytes) goes from 0 to 65535
 */

void setup() {
  Serial.begin(9600);
}

void loop() {
  delay(2000); // 2 seconds
  
  // The car just arrived, it thinks it is alone
  Serial.write("M    Alfa  Giulia  0NLS\n");

  delay(2000); // 2 seconds
  
  // The car sees 3 cars, it has not joined the network
  Serial.write("M    Alfa  Giulia  0NLS\n");
  Serial.write("L0000000000000000 90000\n");
  Serial.write("A0000000000000000180000\n");
  Serial.write("R0000000000000000270000\n");

  delay(2000); // 2 seconds
  
  // The car sees 2 cars and joined the network
  Serial.write("M    Alfa  Giulia  0NLS\n");
  Serial.write("L    Fiat     500 90NAS\n");
  Serial.write("R    Alfa  Giulia270NLL\n");
}
