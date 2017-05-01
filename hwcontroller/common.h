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

typedef enum RequestedAction {
    ERA_STRAIGHT = 'A',
    ERA_TURN_LEFT = 'L',
    ERA_TURN_RIGHT = 'R'
} RequestedAction;

typedef enum CurrentAction {
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
