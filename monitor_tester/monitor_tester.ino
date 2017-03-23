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
 * Size: 385 Bytes (Assuming FFT_N = 128 = 2 * number of bins)
 * AA...AABB...BBCC...CC\n
 * |_____||_____||_____|
 *   128    128    128
 *     
 * A..A   The sensor data relative to the leftmost IR receiver
 * B..B   The sensor data relative to the front IR receiver
 * C..C   The sensor data relative to the rightmost IR receiver
 * 
 *        FIELD NAME         DIM
 * A,B,C  BinFreqIntensity   2B
 * 
 * Each bin represents the intensity of a frequency range. The
 * frequencies go from 0 to sampling_frequency / 2. Bin i represents //TODO: check the end of the range
 * the range [i * sampling_frequency / 128, (i+1) * sampling_frequency / 128]
 * 
 * The intensity (2 Bytes) goes from 0 to 65535
 */

void setup() {
  Serial.begin(9600);
}

void loop() {
  delay(2000); // 2 seconds
  
  // The car just arrived, it thinks it is alone
  Serial.println("MAlfa    Giulia    0NLS\n");
  // Serial.println("");

  delay(2000); // 2 seconds
  
  // The car sees 3 cars, it has not joined the network
  Serial.println("MAlfa    Giulia    0NLS\n");
  Serial.println("L                 90   \n");
  Serial.println("A                180   \n");
  Serial.println("R                270   \n");

  delay(2000); // 2 seconds
  
  // The car sees 2 cars and joined the network
  Serial.println("MAlfa    Giulia    0NLS\n");
  Serial.println("LFiat    500      90NAS\n");
  Serial.println("RAlfa    Giulia  270NLL\n");
}
