void setup() {
  Serial.begin(9600);

  /* ARDUINO-MONITOR MESSAGE FORMAT:
   *
   * ROAD-MESSAGE:
   * Field Name     Dim.
   * Road_ID        1B
   * Manufacturer   8B
   * Model          8B
   * Orientation    1B
   * Priority       1B
   * 
   * ACTION-MESSAGE:
   * 
   */
}

void loop() {
  delay(2000) // 2 seconds
  Serial.write("Serial connected. \n");
}
