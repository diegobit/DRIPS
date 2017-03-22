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
   * FIELDS:
   * Road_ID:
   *          M   my road
   *          L   road to my left
   *          A   road ahead
   *          R   road to my right
   * Manufacturer:
   *          ____Alfa
   *          ____Fiat
   * Model:
   *          __Giulia
   *          _____500
   * Orientation:
   *          0   degress
   *          .
   *          .
   *          360 
   * Priority:
   *          Y   regular car
   *          N   priority car (ambulance, police)
   * RequestedAction / CurrentAction:
   *          S   Stay still
   *          L   turn left
   *          A   go straight ahead
   *          R   turn right
   * 
   * 
   * FREQUENCY-SPECTRUM-MESSAGE:
   * Bins*Int
   * 
   */
}

void loop() {
  delay(2000); // 2 seconds
  Serial.write("R    Alfa  Giulia270NL\n");
}
