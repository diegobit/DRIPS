#ifndef H_COMMON
#define H_COMMON

#define _us 1

#include <Arduino.h>

#define DEBUG 1
#define ADDRESS "1" // Must be 1 character long.
#define MANUFACTURER "ALFARMEO" // Must be 8 characters long
#define MODEL "GIULIA  " // Must be 8 characters long

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
#define SAMPLING_PERIOD (400*_us)

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
const uint8_t LED1_PERIOD     = 10;
const uint8_t LED2_PERIOD     = 20;
const uint8_t LED3_PERIOD     = 30;
const uint8_t LED4_PERIOD     = 40;
const uint8_t LED5_PERIOD     = 50;
const uint8_t LED_CCS_PERIOD  = 60;
const uint16_t LED_TURN_PERIOD = 10000;

const uint8_t LED1_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED1_PERIOD * TIMER_PERIOD);
const uint8_t LED2_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED2_PERIOD * TIMER_PERIOD);
const uint8_t LED3_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED3_PERIOD * TIMER_PERIOD);
const uint8_t LED4_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED4_PERIOD * TIMER_PERIOD);
const uint8_t LED5_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED5_PERIOD * TIMER_PERIOD);
const uint8_t LED_CCS_BIN = (FHT_N * (uint16_t)SAMPLING_PERIOD) / (LED_CCS_PERIOD * TIMER_PERIOD);

typedef enum RequestedAction : char {
    ERA_STRAIGHT = 'A',
    ERA_TURN_LEFT = 'L',
    ERA_TURN_RIGHT = 'R'
} RequestedAction;

typedef enum CurrentAction : char {
    ECA_STRAIGHT = 'A',
    ECA_TURN_LEFT = 'L',
    ECA_TURN_RIGHT = 'R',
    ECA_TURN_STILL = 'S',
} CurrentAction;

typedef struct RoadInfo {
    char manufacturer[8];
    char model[8];
    uint16_t orientation;
    bool priority;
    RequestedAction requestedAction;
    CurrentAction currentAction;
} RoadInfo;

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
