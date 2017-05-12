#ifndef H_COMMON
#define H_COMMON

#define _us 1

#include <Arduino.h>

#define DEBUG 1
const char ADDRESS = '1';
const char MANUFACTURER[8] = { 'A', 'L', 'F', 'A', 'R', 'M', 'E', 'O' };
const char MODEL[8] = { 'G', 'I', 'U', 'L', 'I', 'A', ' ', ' ' };

/**
 * FFT Parameters
 */
#define LOG_OUT 0
#define FHT_N 128 // Numero samples
#define WINDOW 0
#define LIN_OUT 1

/**
 * Sampling period, in microseconds.
 * 
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
#define SAMPLING_PERIOD (470*_us)

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
const uint8_t LED1_PERIOD     = 160;
const uint8_t LED2_PERIOD     = 40;
const uint8_t LED3_PERIOD     = 24;
const uint8_t LED4_PERIOD     = 16;
const uint8_t LED5_PERIOD     = 12;
const uint8_t LED_CCS_PERIOD  = 10;
const uint16_t LED_TURN_PERIOD = 10000;

const uint8_t LED1_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED1_PERIOD * TIMER_PERIOD);
const uint8_t LED2_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED2_PERIOD * TIMER_PERIOD);
const uint8_t LED3_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED3_PERIOD * TIMER_PERIOD);
const uint8_t LED4_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED4_PERIOD * TIMER_PERIOD);
const uint8_t LED5_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED5_PERIOD * TIMER_PERIOD);
const uint8_t LED_CCS_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED_CCS_PERIOD * TIMER_PERIOD);

/**
 * Maximum measured execution time of loop() without handleCCS()
 */
const uint16_t TIMESPAN_LOOP_NOCCS = 218;

/**
 * Maximum measured execution time of handleCCS()
 * Defined as:   max_time{FUN_ST_BEGIN, FUN_ST_WAIT_TO_BLINK, FUN_ST_BLINK, FUN_ST_INTERPRETATE}
 */
const uint16_t TIMESPAN_LOOP_CCSONLY = 0; // FIXME measure it

/**
 * Maximum execution time of a loop()
 */
const uint16_t TIMESPAN_LOOP_MAX = TIMESPAN_LOOP_NOCCS + TIMESPAN_LOOP_CCSONLY;

typedef enum RequestedAction : char {
    ERA_NONE = '0',
    ERA_STRAIGHT = 'A',
    ERA_TURN_LEFT = 'L',
    ERA_TURN_RIGHT = 'R'
} RequestedAction;

typedef enum CurrentAction : char {
    ECA_NONE = '0',
    ECA_STRAIGHT = 'A',
    ECA_TURN_LEFT = 'L',
    ECA_TURN_RIGHT = 'R',
    ECA_STILL = 'S',
} CurrentAction;

typedef struct RoadInfo {
    unsigned long validUntil;
    char manufacturer[8];
    char model[8];
    uint16_t orientation;
    bool priority;
    RequestedAction requestedAction;
    CurrentAction currentAction;
} RoadInfo;

inline bool isValidRequestedAction(int val) {
    return val == ERA_STRAIGHT || val == ERA_TURN_LEFT || val == ERA_TURN_RIGHT;
}

inline bool isValidCurrentAction(int val) {
    return val == ECA_STRAIGHT || val == ECA_TURN_LEFT || val == ECA_TURN_RIGHT || val == ECA_STILL;
}

/**
 * Action advertised by the car
 */
extern RequestedAction requestedAction;

/**
 * The action agreed with the network
 */
extern CurrentAction currentAction;

/**
 * Whether the car should be the first to act on the crossroad
 */
extern bool hasPriority;

/**
 * Current model of the crossroad.
 *
 * Position 0: left road
 * Position 1: front road
 * Position 2: right road
 */
extern RoadInfo crossroad[3];

#if DEBUG
void __assert(bool success, String msg);
#endif

#endif // H_COMMON
