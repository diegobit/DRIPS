/* 
 *  This code sends some test packets through the serial port
 *  to test the communication and the monitor
 */

#define FFT_N 128 // number of samples

uint16_t bins1[FFT_N];
uint16_t bins2[FFT_N];
uint16_t bins3[FFT_N];



void sendPartialInfoMessage(char roadId, uint16_t orientation) {
  Serial.print('I');
  Serial.print(roadId);
  Serial.print("                "); // 16 spaces
  if (orientation < 10) Serial.print(F("  "));
  else if (orientation < 100) Serial.write(' ');
  Serial.print(orientation);
  Serial.print("   ");
  Serial.print('\n');
}

/*
 * Sends an info-message to the serial port
 * Manufacturer and model should be already padded
 */
void sendInfoMessage(char roadId, String manufacturer, String model,
                     uint16_t orientation, char priority, char requestedAction, char currentAction) {

  Serial.print('I');
  Serial.print(roadId);
  Serial.print(manufacturer);
  Serial.print(model);
  if (orientation < 10) Serial.print(F("  "));
  else if (orientation < 100) Serial.write(' ');
  Serial.print(orientation);
  Serial.print(priority);
  Serial.print(requestedAction);
  Serial.print(currentAction);
  Serial.print('\n');
}

void sendFrequencyMessage(char type, uint16_t fft_bins[]) {
  Serial.print(type);
  Serial.print(200);
  for(uint8_t i = 0; i < FFT_N-1; i++) {
    Serial.print(fft_bins[i]);
    Serial.print(',');
  }
  Serial.print(fft_bins[FFT_N-1]);
  Serial.print('\n');
}



void setup() {
  Serial.begin(230400);
  randomSeed(9357);
}

void loop() {
  delay(100000); // 2 seconds
  
//  unsigned long startTime = micros();


  
  /**********
   * The car just arrived, it thinks it is alone (its sensors see nothing)
   **********/   
  sendInfoMessage('M', "Alfa    ", "Giulia  ", 0, 'N', 'L', 'S');

  for (uint16_t i = 0; i < FFT_N; i++) {
    bins1[i] = i + random(21);
    bins2[i] = i + random(21);
    bins3[i] = i + random(21);
  }

  //delay(2000); // 2 seconds
  
  /**********
   * The car sees 3 other cars, it has not joined the network
   **********/
  sendInfoMessage('M', "Alfa    ", "Giulia  ", 0, 'N', 'L', 'S');
  sendPartialInfoMessage('L', 90);
  sendPartialInfoMessage('A', 180);
  sendPartialInfoMessage('R', 270);

  bins1[9] = 700 + random(21);
  bins1[10] = 700 + random(21);
  bins1[11] = 700 + random(21);
  bins2[20] = 400 + random(21);
  bins2[21] = 400 + random(21);
  bins2[22] = 400 + random(21);
  bins3[30] = 900 + random(21);
  bins3[31] = 900 + random(21);
  bins3[32] = 900 + random(21);
  sendFrequencyMessage('L', bins1);
  sendFrequencyMessage('F', bins2);
  sendFrequencyMessage('R', bins3);

  //delay(2000); // 2 seconds
  
  /**********
   * The car sees 3 other cars and joined the network
   **********/
  sendInfoMessage('M', "Alfa    ", "Giulia  ",   0, 'N', 'L', 'S');
  sendInfoMessage('L', "Fiat    ", "500     ",  90, 'N', 'A', 'S');
  sendInfoMessage('R', "Tesla   ", "Model S ", 270, 'N', 'L', 'L');

  

//  unsigned long time = micros() - startTime;
//  Serial.write("STATS: Time passed is (us): ");
//  Serial.println(time); 
  
}

/************
 * TESTS
 ************
 * Time to send 8 Info-messages and 3 frequency-spectrum-messages: 30,812 ms
 * Time to send a single frequency-spectrum-message: 7,496 ms 
 * (slightly old message format)
 * 
 * 'Serial.write("IMAlfa    Giulia    0NLS\n");' 500 times takes 559604 us
 * sendInfoMessage(...)                                    takes 562504 us
 */
