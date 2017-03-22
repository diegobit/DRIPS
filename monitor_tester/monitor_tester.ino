void setup() {
  Serial.begin(9600);

  /* ARDUINO-MONITOR MESSAGE FORMAT:
   *
   * INFO-MESSAGE:
   * 
   * FIELD NAME         DIM   LETTER    
   * Road_ID            1B    A
   * Manufacturer       8B    B
   * Model              8B    C
   * Orientation        3B    D
   * Priority           1B    E
   * RequestedAction    1B    F   // The action the car wants to accomplish
   * CurrentAction      1B    G   // The action the car is doing to comply with what the network requires
   * 
   * ABBBBBBBBCCCCCCCCDDDEFG\n
   * 
   * 
   * FREQUENCY-SPECTRUM-MESSAGE:
   * Bins*Int
   * 
   */
}

void loop() {
  delay(2000); // 2 seconds
  Serial.write("---MESSAGE---\n");
}
