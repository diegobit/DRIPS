#define _us 1

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
#define FASTADC 1
// defines for setting and clearing register bits
#ifndef cbi
#define cbi(sfr, bit) (_SFR_BYTE(sfr) &= ~_BV(bit))
#endif
#ifndef sbi
#define sbi(sfr, bit) (_SFR_BYTE(sfr) |= _BV(bit))
#endif

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
#define SAMPLING_PERIOD   2
#define LED1_PERIOD       2
#define LED2_PERIOD       20
#define LED3_PERIOD       30
#define LED4_PERIOD       40
#define LED5_PERIOD       50
#define LED_TURN_PERIOD   10000

uint8_t LED1_COUNTER = 0;
uint8_t LED2_COUNTER = 0;
uint8_t LED3_COUNTER = 0;
uint8_t LED4_COUNTER = 0;
uint8_t LED5_COUNTER = 0;
uint16_t LED_TURN_COUNTER = 0;

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

void timerHandler() {

  FLASH_IR_LED(LED1_COUNTER, LED1_PERIOD, IR_LED_1);
  FLASH_IR_LED(LED2_COUNTER, LED2_PERIOD, IR_LED_2);
  FLASH_IR_LED(LED3_COUNTER, LED3_PERIOD, IR_LED_3);
  FLASH_IR_LED(LED4_COUNTER, LED4_PERIOD, IR_LED_4);
  FLASH_IR_LED(LED5_COUNTER, LED5_PERIOD, IR_LED_5);

  FLASH_TURN_LED(LED_TURN_COUNTER, LED_TURN_PERIOD, TURN_L, TURN_R);
}


void setup() {
  #if FASTADC
   // set prescale to 16
   sbi(ADCSRA,ADPS2) ;
   cbi(ADCSRA,ADPS1) ;
   cbi(ADCSRA,ADPS0) ;
  #endif

  Serial.begin(9600);

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
//      RF24::printBuffer("request: ", buf, len);
      Serial.print(F("got request: "));
      Serial.println((char*)buf);
//      Serial.print("RSSI: ");
//      Serial.println((uint8_t)rf24.lastRssi(), DEC);
      
      // Send a reply
      uint8_t data[] = "Hello Back";
      //uint8_t data[100];
      //String(millis()).toCharArray(data, 100);
      nrf24.send(data, sizeof(data));
      nrf24.waitPacketSent();
      Serial.println(F("Sent a reply"));
    }
    else
    {
      Serial.println(F("recv failed"));
    }
  }  
}

void loop() {
  static uint8_t buttonMillis = 0;

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

  /*Serial.println(F("--begin-fft--"));

  for (int i = 0; i < FFT_N/2; i++) {
    Serial.print(i * (float)SAMPLING_FREQ / FFT_N);
    Serial.print(" -> ");
    Serial.print((i+1) * (float)SAMPLING_FREQ / FFT_N);
    Serial.print(" = ");
    Serial.println(fft_lin_out[i]);
  }

  Serial.println(F("--end-fft--"));*/
  if (analogRead(BUTTON) < 512) {
    buttonPressed = true;
    //buttonMillis = (millis() / 100) TODO
    visibleAction++;
    if (visibleAction > PRIORITY) {
      visibleAction = NONE;
    }
  } else {
    if (buttonPressed) {
      requestedAction = visibleAction;
      buttonPressed = false;
    }
  }

  //Serial.write(fft_log_out, 128); // send out the data

  test_radio();

  // Avoid choking the serial buffer
  delay(500);
}
