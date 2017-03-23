#define _us 1

#define LOG_OUT 0
#define FFT_N 128 // Numero samples
#define WINDOW 0
#define LIN_OUT 1

#include <FFT.h>
#include <FlexiTimer2.h>

#define IR_LED_1 8
#define IR_LED_2 10
#define IR_LED_3 9
#define IR_LED_4 9
#define IR_LED_5 9
#define SENSOR   A0

/**
 * The sampling frequency must be high enough to be able to read the signal multiple times, and
 * must be low enough to be able to read enough samples to see an entire period of the signal:
 * 
 *  · SAMPLING_FREQ  >=  2 * SIGNAL_MAX_FREQ
 *  · 1/SAMPLING_FREQ * FFT_N  >=  2 * 1/SIGNAL_MIN_FREQ
 *  
 *  In other terms, you must satisfy these two inequalities:
 *  
 *  · SAMPLING_FREQ  >=  2 * SIGNAL_MAX_FREQ
 *  · SAMPLING_FREQ  <=  SIGNAL_MIN_FREQ * FFT_N / 2
 *  
 */
#define SAMPLING_FREQ 1000 // Hz

/**
 * Basic frequency in µs for the timer.
 */
#define TIMER_PERIOD (100*_us)

/*
 * Unit: TIMER_PERIOD. These values MUST be even.
 *
 * Example of what happens with value 4:
 *
 *    0 ____ 1 ____ 2 ^^^^ 3 ^^^^ 0
 *
 * (between each number there is a delay of TIMER_PERIOD)
*/
#define SAMPLING_PERIOD 2
#define LED1_PERIOD     10
#define LED2_PERIOD     20
#define LED3_PERIOD     30
#define LED4_PERIOD     40
#define LED5_PERIOD     50

uint8_t LED1_COUNTER = 0;
uint8_t LED2_COUNTER = 0;
uint8_t LED3_COUNTER = 0;
uint8_t LED4_COUNTER = 0;
uint8_t LED5_COUNTER = 0;

void flash() {

  if (LED1_COUNTER == (LED1_PERIOD/2) - 1) {
    digitalWrite(IR_LED_1, HIGH);
    LED1_COUNTER++;
  } else if (LED1_COUNTER == LED1_PERIOD - 1) {
    digitalWrite(IR_LED_1, LOW);
    LED1_COUNTER = 0;
  } else {
    LED1_COUNTER++;
  }

  if (LED2_COUNTER == (LED2_PERIOD/2) - 1) {
    digitalWrite(IR_LED_2, HIGH);
    LED2_COUNTER++;
  } else if (LED2_COUNTER == LED2_PERIOD - 1) {
    digitalWrite(IR_LED_2, LOW);
    LED2_COUNTER = 0;
  } else {
    LED2_COUNTER++;
  }

  if (LED3_COUNTER == (LED3_PERIOD/2) - 1) {
    digitalWrite(IR_LED_3, HIGH);
    LED3_COUNTER++;
  } else if (LED3_COUNTER == LED3_PERIOD - 1) {
    digitalWrite(IR_LED_3, LOW);
    LED3_COUNTER = 0;
  } else {
    LED3_COUNTER++;
  }

  if (LED4_COUNTER == (LED4_PERIOD/2) - 1) {
    digitalWrite(IR_LED_4, HIGH);
    LED4_COUNTER++;
  } else if (LED4_COUNTER == LED4_PERIOD - 1) {
    digitalWrite(IR_LED_4, LOW);
    LED4_COUNTER = 0;
  } else {
    LED4_COUNTER++;
  }

  if (LED5_COUNTER == (LED5_PERIOD/2) - 1) {
    digitalWrite(IR_LED_5, HIGH);
    LED5_COUNTER++;
  } else if (LED5_COUNTER == LED5_PERIOD - 1) {
    digitalWrite(IR_LED_5, LOW);
    LED5_COUNTER = 0;
  } else {
    LED5_COUNTER++;
  }

}


void setup() {
  Serial.begin(9600);
  pinMode(SENSOR, INPUT);
  pinMode(IR_LED_1, OUTPUT);
  pinMode(IR_LED_2, OUTPUT);
  pinMode(IR_LED_3, OUTPUT);
  pinMode(IR_LED_4, OUTPUT);
  pinMode(IR_LED_5, OUTPUT);

  unsigned long semiperiod = TIMER_PERIOD / 2;
  FlexiTimer2::set(semiperiod / 100, 1.0/10000, flash); // max resolution appears to be 100 µs. 10 µs is distorted, while 1 µs is broken.
  FlexiTimer2::start();
}

void loop() {
  Serial.println("\n *** Reading start ***\n");

  // FIXME Move to flash()
  for (int i = 0 ; i < FFT_N*2 ; i += 2) { // save 256 samples
    fft_input[i] = analogRead(SENSOR); // put real data into even bins
    fft_input[i+1] = 0; // set odd bins to 0
    delayMicroseconds(1000000 / SAMPLING_FREQ); // in micros
  }

  Serial.println("\n *** Reading end ***\n");
  
  // window data, then reorder, then run, then take output
  //fft_window(); // window the data for better frequency response
  fft_reorder(); // reorder the data before doing the fft
  fft_run(); // process the data in the fft
  fft_mag_lin(); // take the output of the fft

  Serial.println(F("--begin-fft--"));

  for (int i = 0; i < FFT_N/2; i++) {
    Serial.print(i * (float)SAMPLING_FREQ / FFT_N);
    Serial.print(" -> ");
    Serial.print((i+1) * (float)SAMPLING_FREQ / FFT_N);
    Serial.print(" = ");
    Serial.println(fft_lin_out[i]);
  }

  Serial.println(F("--end-fft--"));

  //Serial.write(fft_log_out, 128); // send out the data

  // Avoid choking the serial buffer
  delay(500);
}
