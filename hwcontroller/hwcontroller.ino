#pragma GCC optimize ("O3")
#include "common.h"
#include "ccs.h"

/**
 * Includes
 */
#include <FHT.h>
#include <FlexiTimer2.h>

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
#define SENSOR_L A1
#define SENSOR_F A2
#define SENSOR_R A3
#define BUTTON   A0
#define RND_SEED A4 // Unconnected pin used as a seed for the RNG

/**
 * Configuration parameters
 */
#define TURN_BUTTON_DELAY 30  // Delay of the turn button, in tenths of a second.

// Define various ADC prescaler
const unsigned char PS_16 = (1 << ADPS2);
const unsigned char PS_32 = (1 << ADPS2) | (1 << ADPS0);
const unsigned char PS_64 = (1 << ADPS2) | (1 << ADPS1);
const unsigned char PS_128 = (1 << ADPS2) | (1 << ADPS1) | (1 << ADPS0);

typedef enum VisibleAction : uint8_t {
    EVA_STRAIGHT = 0,
    EVA_TURN_LEFT = 1,
    EVA_TURN_RIGHT = 2,
    EVA_PRIORITY = 3
} VisibleAction;


// FHT outputs
static uint16_t fhtLeft[FHT_N / 2];   // Allocate the space
static uint16_t fhtFront[FHT_N / 2];  // Allocate the space
static uint16_t *fhtRight;            // Don't allocate space as we'll use this just as a reference to fht_lin_out

// Action shown by the turn leds (can be different from requestedAction while the
// user is switching through the actions with the button)
VisibleAction visibleAction = EVA_STRAIGHT;

bool buttonPressed = false;

/**
 * The type of these must be such that its greatest value
 * is > max(LED_CCS_PERIOD, LED1_PERIOD, ..., LED5_PERIOD)
 */
uint8_t LED1_COUNTER = 0;
uint8_t LED2_COUNTER = 0;
uint8_t LED3_COUNTER = 0;
uint8_t LED4_COUNTER = 0;
uint8_t LED5_COUNTER = 0;

/**
 * The type of this must be such that its greatest value
 * is > LED_TURN_PERIOD
 */
uint16_t LED_TURN_COUNTER = 0;

#define FLASH_IR_LED(counter, period, pin) {\
    if (counter == ((period)/2) - 1) {\
        digitalWrite((pin), HIGH);\
        counter++;\
    } else if (counter >= (period) - 1) {\
        digitalWrite((pin), LOW);\
        counter = 0;\
    } else {\
        counter++;\
    }\
}

#define FLASH_TURN_LED(counter, period, pinL, pinR) {\
    if (counter == ((period)/2) - 1) {\
        if (visibleAction == EVA_TURN_LEFT) {\
            digitalWrite((pinL), HIGH);\
            digitalWrite((pinR), LOW);\
        } else if (visibleAction == EVA_TURN_RIGHT) {\
            digitalWrite((pinL), LOW);\
            digitalWrite((pinR), HIGH);\
        } else if (visibleAction == EVA_PRIORITY) {\
            digitalWrite((pinL), HIGH);\
            digitalWrite((pinR), HIGH);\
        } else {\
            digitalWrite((pinL), LOW);\
            digitalWrite((pinR), LOW);\
        }\
        counter++;\
    } else if (counter >= (period) - 1) {\
        if (visibleAction == EVA_TURN_LEFT) {\
            digitalWrite((pinL), LOW);\
            digitalWrite((pinR), LOW);\
        } else if (visibleAction == EVA_TURN_RIGHT) {\
            digitalWrite((pinL), LOW);\
            digitalWrite((pinR), LOW);\
        } else if (visibleAction == EVA_PRIORITY) {\
            digitalWrite((pinL), LOW);\
            digitalWrite((pinR), LOW);\
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

    if (advertiseCCS) {
        FLASH_IR_LED(LED1_COUNTER, LED_CCS_PERIOD, IR_LED_1);
        FLASH_IR_LED(LED2_COUNTER, LED_CCS_PERIOD, IR_LED_2);
        FLASH_IR_LED(LED3_COUNTER, LED_CCS_PERIOD, IR_LED_3);
        FLASH_IR_LED(LED4_COUNTER, LED_CCS_PERIOD, IR_LED_4);
        FLASH_IR_LED(LED5_COUNTER, LED_CCS_PERIOD, IR_LED_5);
    } else {
        FLASH_IR_LED(LED1_COUNTER, LED1_PERIOD, IR_LED_1);
        FLASH_IR_LED(LED2_COUNTER, LED2_PERIOD, IR_LED_2);
        FLASH_IR_LED(LED3_COUNTER, LED3_PERIOD, IR_LED_3);
        FLASH_IR_LED(LED4_COUNTER, LED4_PERIOD, IR_LED_4);
        FLASH_IR_LED(LED5_COUNTER, LED5_PERIOD, IR_LED_5);
    }

    FLASH_TURN_LED(LED_TURN_COUNTER, LED_TURN_PERIOD, TURN_L, TURN_R);
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

        // The cast to uint8_t is the same as taking the positive modulo:
        //                            (uint8_t)x
        //                                ==
        //                              x % 256
        if ((uint8_t)(curMillis - buttonMillis) >= TURN_BUTTON_DELAY) {
            buttonMillis = curMillis;
            switch (visibleAction) {
                case EVA_STRAIGHT:
                    visibleAction = EVA_TURN_LEFT;
                    break;
                case EVA_TURN_LEFT:
                    visibleAction = EVA_TURN_RIGHT;
                    break;
                case EVA_TURN_RIGHT:
                    visibleAction = EVA_PRIORITY;
                    break;
                case EVA_PRIORITY:
                    visibleAction = EVA_STRAIGHT;
                    break;
            }
        }
    } else {
        // Button is not currently held down
        if (buttonPressed) {
            switch (visibleAction) {
                case EVA_STRAIGHT:
                    requestedAction = ERA_STRAIGHT;
                    hasPriority = false;
                    break;
                case EVA_TURN_LEFT:
                    requestedAction = ERA_TURN_LEFT;
                    hasPriority = false;
                    break;
                case EVA_TURN_RIGHT:
                    requestedAction = ERA_TURN_RIGHT;
                    hasPriority = false;
                    break;
                case EVA_PRIORITY:
                    hasPriority = true;
                    break;
            }
            buttonPressed = false;
        }
    }
}

/**
 * Send a "sample" message on the serial port.
 *
 * @param type  Type of the message ('l', 'f', or 'r')
 * @param data  Pointer to an array of samples, of length FHT_N
 */
void sendSamplesMessage(char type, int *data) {
    Serial.print(type);
    Serial.print(SAMPLING_PERIOD);
    Serial.print(';');
    for (uint8_t i = 0; i < FHT_N - 1; i++) {
        Serial.print(data[i]);
        Serial.print(',');
    }
    Serial.print(data[FHT_N - 1]);
    Serial.print('\n');
}

/**
 * Send a "frequency" message on the serial port.
 *
 * @param type  Type of the message ('L', 'F', or 'R')
 * @param data  Pointer to an array of samples, of length FHT_N/2
 */
void sendFrequencyMessage(char type, uint16_t *data) {
    Serial.print(type);
    Serial.print(SAMPLING_PERIOD);
    Serial.print(';');
    for (uint8_t i = 0; i < FHT_N / 2 - 1; i++) {
        Serial.print(data[i]);
        Serial.print(',');
    }
    Serial.print(data[FHT_N / 2 - 1]);
    Serial.print('\n');
}

void fht_constant_detrend() {
    uint16_t mean = 0;
    for (uint8_t i = 0; i < FHT_N; i++) {
        mean += fht_input[i];
    }
    mean = mean / FHT_N;
    for (uint8_t i = 0; i < FHT_N; i++) {
        fht_input[i] -= mean;
    }
}

void fht_denoise() {
    uint16_t noise = 0;

    uint8_t bins[6] = { LED1_BIN, LED2_BIN, LED3_BIN, LED4_BIN, LED5_BIN, LED_CCS_BIN };
    for (uint8_t i = 1; i < 6; i++) {
        noise = max(noise, fht_lin_out[(bins[i-1] + bins[i]) / 2]);
    }

    for (uint8_t i = 0; i < FHT_N / 2; i++) {
        fht_lin_out[i] -= noise;
    }
}

/**
 * @param pin           Pin number of the IR sensor
 * @param sampleMsgType Type of the sampling messages ('l', 'f', or 'r')
 * @param freqMsgType   Type of the spectrum messages ('L', 'F', or 'R')
 * @param output        Pointer to an array of length (FHT_N/2) in which the result will be copied.
 *                      If NULL, the result will be a reference to `fht_lin_out`.
 *
 * @return       A reference to the output array, which can be `output` or `fht_lin_out` depending on
 *               the `output` parameter.
 */
uint16_t *readIrFrequencies(uint8_t pin, char sampleMsgType, char freqMsgType, uint16_t *output) {
    // We flush the serial just before the sampling, so that we don't have unnecessary interrupts ruining our timing.
    Serial.flush();

    // Sampling
    unsigned long timing = micros();
    fht_input[0] = analogRead(pin);
    for (uint16_t i = 1; i < FHT_N; i++) {
        unsigned long deadline = timing + i * SAMPLING_PERIOD;
        while (micros() < deadline);

        fht_input[i] = analogRead(pin);
    }


    #if DEBUG
        sendSamplesMessage(sampleMsgType, fht_input);
    #endif

    fht_constant_detrend();
    // window data, then reorder, then run, then take output
    #if WINDOW
        fht_window(); // window the data for better frequency response
    #endif
    fht_reorder(); // reorder the data before doing the fft
    fht_run(); // process the data in the fft
    fht_mag_lin(); // take the output of the fft

    sendFrequencyMessage(freqMsgType, fht_lin_out);

    fht_denoise(); // remove noise from output

    if (output != NULL) {
        memcpy(output, fht_lin_out, sizeof(fht_lin_out));
        return output;
    }

    return fht_lin_out;
}

void interpretateSensorData(uint16_t *left, uint16_t *front, uint16_t *right) {
    // TODO look for frequencies and build a representation of the crossroad
}


void setup() {
    // set up the ADC
    ADCSRA &= ~PS_128;  // remove bits set by Arduino library

    // you can choose a prescaler from above.
    // PS_16, PS_32, PS_64 or PS_128
    ADCSRA |= PS_16;    // set our own prescaler to 16


    Serial.begin(230400);

    randomSeed(analogRead(RND_SEED));

    setupCCS(fhtLeft, fhtFront, fhtRight);

    pinMode(SENSOR_L, INPUT);
    pinMode(SENSOR_F, INPUT);
    pinMode(SENSOR_R, INPUT);
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

void loop() {
    readIrFrequencies(SENSOR_L, 'l', 'L', fhtLeft);

    handleTurnButton();

    readIrFrequencies(SENSOR_F, 'f', 'F', fhtFront);

    handleTurnButton();

    fhtRight = readIrFrequencies(SENSOR_R, 'r', 'R', NULL);

    handleTurnButton();

    // Run right after the FHTs, for better timing
    handleCCS();

    handleTurnButton();

    interpretateSensorData(fhtLeft, fhtFront, fhtRight);

    handleTurnButton();

    delay(100);
}
