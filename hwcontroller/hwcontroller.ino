#pragma GCC optimize ("O3")
#define _us 1

#define DEBUG 1
#if DEBUG
void __assert(bool success, String msg) {
  if (!success) {
    Serial.println(msg);
    Serial.flush();
    abort();
  }
}
#endif

/**
 * FFT Parameters
 */
#define LOG_OUT 0
#define FFT_N 128 // Numero samples
#define WINDOW 0
#define LIN_OUT 1

/**
 * Includes
 */
#include <FFT.h>
#include <FlexiTimer2.h>
#include <SPI.h>       // nRF24L01+
#include <RH_NRF24.h>  // nRF24L01+

/**
 * Ports
 */
#define IR_LED_1 2
#define IR_LED_2 3
#define IR_LED_3 4
#define IR_LED_4 5
#define IR_LED_5 6
#define TURN_L   7
#define TURN_R   8
#define SENSOR   A1
#define BUTTON   A0

/**
 * Configuration parameters
 */
#define TURN_BUTTON_DELAY 30  // Delay of the turn button, in tenths of a second.

/**
 * analogRead() is slow (more than 100 µs per call).
 * We try to make it faster by setting the prescale to 16. In this way we get a speed of 16 µs per call, which
 * means a sample rate of ~62.5KHz. We lose, however, on accuracy.
 * More info: http://forum.arduino.cc/index.php?topic=6549.0
 *
 *  Prescaler   Maximum sampling frequency
 *     16            62.5 kHz
 *     32            33.2 kHz
 *     64            17.8 kHz
 *    128             8.9 kHz
 *
 * To disable, set FASTADC to 0
 */
#define FASTADC 0
// defines for setting and clearing register bits
#ifndef cbi
#define cbi(sfr, bit) (_SFR_BYTE(sfr) &= ~_BV(bit))
#endif
#ifndef sbi
#define sbi(sfr, bit) (_SFR_BYTE(sfr) |= _BV(bit))
#endif

// Define various ADC prescaler
const unsigned char PS_16 = (1 << ADPS2);
const unsigned char PS_32 = (1 << ADPS2) | (1 << ADPS0);
const unsigned char PS_64 = (1 << ADPS2) | (1 << ADPS1);
const unsigned char PS_128 = (1 << ADPS2) | (1 << ADPS1) | (1 << ADPS0);

// Singleton instance of the radio driver
RH_NRF24 nrf24(10, 9); // CE, CS

enum ACTIONS {
  NONE = 0,
  TURN_LEFT = 1,
  TURN_RIGHT = 2,
  PRIORITY = 3
};

uint8_t requestedAction = NONE; // Actual action advertised by the car
uint8_t visibleAction = NONE; // Action shown by the turn leds
bool buttonPressed = false;

/**
 * Basic frequency in µs for the timer.
 */
#define TIMER_PERIOD (100*_us)

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

/*
 * Unit: TIMER_PERIOD. These values MUST be even.
 *
 * Example of what happens with value 4:
 *
 *    0 ____ 1 ____ 2 ^^^^ 3 ^^^^ 0
 *
 * (between each number there is a delay of TIMER_PERIOD)
*/
#define SAMPLING_PERIOD   5
#define LED1_PERIOD       10
#define LED2_PERIOD       20
#define LED3_PERIOD       30
#define LED4_PERIOD       40
#define LED5_PERIOD       50
#define LED_TURN_PERIOD   10000

uint16_t SAMPLING_COUNTER = 0;
uint16_t LED1_COUNTER = 0;
uint16_t LED2_COUNTER = 0;
uint8_t LED3_COUNTER = 0;
uint8_t LED4_COUNTER = 0;
uint8_t LED5_COUNTER = 0;
uint16_t LED_TURN_COUNTER = 0;

uint8_t SAMPLING_INDEX = 0;
bool shouldDoFFT = false;

#define DO_SAMPLING(counter, period, pin, sampling_index, should_do_fft) {\
  static bool isSemiperiod = true;\
  if (!should_do_fft && !isSemiperiod) {\
    if (counter == (period) - 1) {\
      fft_input[2 * sampling_index] = analogRead(pin);\
      fft_input[2 * sampling_index + 1] = 0;\
      \
      if (2 * sampling_index + 1 >= FFT_N - 1) {\
        sampling_index = 0;\
        shouldDoFFT = true;\
      } else {\
        sampling_index++;\
      }\
      counter = 0;\
    } else {\
      counter++;\
    }\
  }\
  isSemiperiod = !isSemiperiod;\
}

#define FLASH_IR_LED(counter, period, pin) {\
  if (counter == ((period)/2) - 1) {\
    digitalWrite((pin), HIGH);\
    counter++;\
  } else if (counter == (period) - 1) {\
    digitalWrite((pin), LOW);\
    counter = 0;\
  } else {\
    counter++;\
  }\
}

#define FLASH_TURN_LED(counter, period, pinL, pinR) {\
  if (counter == ((period)/2) - 1) {\
    if (visibleAction == TURN_LEFT) {\
      digitalWrite((pinL), HIGH);\
      digitalWrite((pinR), LOW);\
    } else if (visibleAction == TURN_RIGHT) {\
      digitalWrite((pinL), LOW);\
      digitalWrite((pinR), HIGH);\
    } else if (visibleAction == PRIORITY) {\
      digitalWrite((pinL), HIGH);\
      digitalWrite((pinR), HIGH);\
    } else {\
      digitalWrite((pinL), LOW);\
      digitalWrite((pinR), LOW);\
    }\
    counter++;\
  } else if (counter == (period) - 1) {\
    if (visibleAction == TURN_LEFT) {\
      digitalWrite((pinL), LOW);\
      digitalWrite((pinR), LOW);\
    } else if (visibleAction == TURN_RIGHT) {\
      digitalWrite((pinL), LOW);\
      digitalWrite((pinR), LOW);\
    } else if (visibleAction == PRIORITY) {\
      digitalWrite((pinL), HIGH);\
      digitalWrite((pinR), HIGH);\
    } else {\
      digitalWrite((pinL), LOW);\
      digitalWrite((pinR), LOW);\
    }\
    counter = 0;\
  } else {\
    counter++;\
  }\
}

/**
 * Timer handler which get called every TIMER_PERIOD. This function is executed as
 * the handler of an interrupt, so it is essential to return as quickly as possible.
 *
 * Current GCC optimization level is O3 (optimized for speed).
 * Further speed improvement can be obtained by using direct port manipulation instead of
 * digitalWrite(), as described here: https://www.arduino.cc/en/Reference/PortManipulation
 */
__attribute__((optimize("O3"))) void timerHandler() {

  FLASH_IR_LED(LED1_COUNTER, LED1_PERIOD, IR_LED_1);
  FLASH_IR_LED(LED2_COUNTER, LED2_PERIOD, IR_LED_2);
  FLASH_IR_LED(LED3_COUNTER, LED3_PERIOD, IR_LED_3);
  FLASH_IR_LED(LED4_COUNTER, LED4_PERIOD, IR_LED_4);
  FLASH_IR_LED(LED5_COUNTER, LED5_PERIOD, IR_LED_5);

  FLASH_TURN_LED(LED_TURN_COUNTER, LED_TURN_PERIOD, TURN_L, TURN_R);

  //DO_SAMPLING(SAMPLING_COUNTER, SAMPLING_PERIOD, SENSOR, SAMPLING_INDEX, shouldDoFFT);
}

void setup() {
  #if FASTADC
   // set prescale to 16
   sbi(ADCSRA,ADPS2) ;
   cbi(ADCSRA,ADPS1) ;
   cbi(ADCSRA,ADPS0) ;
  #endif

  // set up the ADC
  ADCSRA &= ~PS_128;  // remove bits set by Arduino library

  // you can choose a prescaler from above.
  // PS_16, PS_32, PS_64 or PS_128
  ADCSRA |= PS_16;    // set our own prescaler to 64 


  Serial.begin(230400);

  if (!nrf24.init())
    Serial.println(F("Radio init failed!"));
  
  pinMode(SENSOR, INPUT);
  pinMode(BUTTON, INPUT);
  pinMode(IR_LED_1, OUTPUT);
  pinMode(IR_LED_2, OUTPUT);
  pinMode(IR_LED_3, OUTPUT);
  pinMode(IR_LED_4, OUTPUT);
  pinMode(IR_LED_5, OUTPUT);
  pinMode(TURN_L, OUTPUT);
  pinMode(TURN_R, OUTPUT);

  // Enable internal pull-up resistor
  digitalWrite(BUTTON, HIGH);

  unsigned long semiperiod = TIMER_PERIOD / 2;
  FlexiTimer2::set(semiperiod / 100, 1.0/10000, timerHandler); // max resolution appears to be 100 µs. 10 µs is distorted, while 1 µs is broken.
  FlexiTimer2::start();
}

void test_radio() {
  if (nrf24.available())
  {
    // Should be a message for us now   
    uint8_t buf[RH_NRF24_MAX_MESSAGE_LEN];
    uint8_t len = sizeof(buf);
    if (nrf24.recv(buf, &len))
    {
      // RF24::printBuffer("request: ", buf, len);
      Serial.print(F("got request: "));
      Serial.println((char*)buf);
      // Serial.print("RSSI: ");
      // Serial.println((uint8_t)rf24.lastRssi(), DEC);
      
      // Send a reply
      uint8_t data[] = "Hello Back";
      nrf24.send(data, sizeof(data));
      nrf24.waitPacketSent();
      Serial.println(F("Sent a reply"));
    } else {
      Serial.println(F("recv failed"));
    }
  }  
}

inline uint8_t positive_modulo(uint8_t i, uint8_t n) {
    return (i % n + n) % n;
}

/**
 * Detects if the turn button has been pressed and controls
 * the changes of the visibleAction and requestedAction.
 *
 * Must be called periodically.
 */
void handleTurnButton() {
  static uint8_t buttonMillis = 0;

  if (digitalRead(BUTTON) == LOW) {
    // Button is currently held down

    // This variable is declared to optimize for speed.
    // You can safely replace each occurrence of curMillis
    // with its expression in order to save memory.
    uint8_t curMillis = (millis() / 100) & 0xFF;

    if (!buttonPressed) {
      // Start counting. Subtract TURN_BUTTON_DELAY so that
      // the user doesn't have to wait for the current action.
      buttonMillis = curMillis - TURN_BUTTON_DELAY;
      buttonPressed = true;
    }

    if (positive_modulo(curMillis - buttonMillis, 256) >= TURN_BUTTON_DELAY) {
      buttonMillis = curMillis;
      visibleAction++;
      if (visibleAction > PRIORITY) {
        visibleAction = NONE;
      }
    }
  } else {
    // Button is not currently held down
    if (buttonPressed) {
      requestedAction = visibleAction;
      buttonPressed = false;
    }
  }
}

void sendFrequencyMessage(char type) {
  Serial.print(type);
  Serial.print(SAMPLING_PERIOD * TIMER_PERIOD);
  Serial.print(';');
  for(uint8_t i = 0; i < FFT_N / 2 - 1; i++) {
    Serial.print(fft_lin_out[i]);
    Serial.print(',');
  }
  Serial.print(fft_lin_out[FFT_N / 2 - 1]);
  Serial.print('\n');
}

void fft_constant_detrend() {
  uint16_t mean = 0;
  for (uint16_t i = 0; i < FFT_N * 2; i += 2) {
    mean += fft_input[i];
  }
  mean = mean / FFT_N;
  for (uint16_t i = 0; i < FFT_N * 2; i += 2) {
    fft_input[i] -= mean;
  }
}

void loop() {
  unsigned long s1 = micros();
  for (int i = 0 ; i < FFT_N*2 ; i += 2) { // save 256 samples
    if (i > 0) {
      //delayMicroseconds(SAMPLING_PERIOD * TIMER_PERIOD);
      //delay(10);
      delayMicroseconds(416); // Equivale a un periodo reale di 500us, ovvero 2000Hz
    }

    fft_input[i] = analogRead(SENSOR); // put real data into even bins
    fft_input[i+1] = 0; // set odd bins to 0
  }
  
  //fft_constant_detrend();
  // window data, then reorder, then run, then take output
  //fft_window(); // window the data for better frequency response
  fft_reorder(); // reorder the data before doing the fft
  fft_run(); // process the data in the fft
  fft_mag_lin(); // take the output of the fft
  
  sendFrequencyMessage('L');

  //Serial.write(fft_log_out, 128); // send out the data

  handleTurnButton();
  test_radio();
}
