#define LOG_OUT 0
#define FFT_N 128 // Numero samples
#define WINDOW 0
#define LIN_OUT 1

#include <FFT.h>
#include <MsTimer2.h>

#define IR_LED_R 8
#define IR_LED_L 10
#define SENSOR A0

void flash() {
  static boolean output = HIGH;
  static uint8_t countL = 0;
  
  digitalWrite(IR_LED_R, output);
  output = !output;

  if (countL == 0) {
    digitalWrite(IR_LED_L, HIGH);
  } else if (countL == 2) {
    digitalWrite(IR_LED_L, LOW);
  }

  if (countL == 3) {
    countL = 0;
  } else {
    countL++;
  }
}


void setup() {
  Serial.begin(9600);
  pinMode(SENSOR, INPUT);
  pinMode(IR_LED_L, OUTPUT);
  pinMode(IR_LED_R, OUTPUT);

  MsTimer2::set(25, flash); // 500ms period
  MsTimer2::start();
}

void loop() {
  Serial.println("\n *** Reading start ***\n");

  for (int i = 0 ; i < FFT_N*2 ; i += 2) { // save 256 samples
    fft_input[i] = analogRead(SENSOR); // put real data into even bins
    fft_input[i+1] = 0; // set odd bins to 0
    delay(5);
  }
  /*for (uint8_t i = 0; i < samples; i++) {
    vReal[i] = analogRead(SENSOR);
    vImag[i] = 0;
    delay(10);
  }*/
  Serial.println("\n *** Reading end ***\n");
  
  // window data, then reorder, then run, then take output
  //fft_window(); // window the data for better frequency response
  fft_reorder(); // reorder the data before doing the fft
  fft_run(); // process the data in the fft
  fft_mag_lin(); // take the output of the fft
  //Serial.write(255); // send a start byte
  float Fs = 200;
  for (int i = 0; i < FFT_N/2; i++) {
    Serial.print(i * Fs/FFT_N);
    Serial.print(" -> ");
    Serial.print((i+1) * Fs/FFT_N);
    Serial.print(" = ");
    Serial.println(fft_lin_out[i]);
  }
  //Serial.write(fft_log_out, 128); // send out the data

}
