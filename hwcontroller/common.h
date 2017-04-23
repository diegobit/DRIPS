#ifndef H_COMMON
#define H_COMMON

#include <Arduino.h>

#define DEBUG 1
#define ADDRESS "1" // Must be 1 character long.
#define MANUFACTURER "ALFARMEO" // Must be 8 characters long
#define MODEL "GIULIA  " // Must be 8 characters long

enum Actions {
    EA_NONE = 0,
    EA_TURN_LEFT = 1,
    EA_TURN_RIGHT = 2,
    EA_PRIORITY = 3
};

typedef struct RoadInfo {
    char manufacturer[8];
    char model[8];
    uint16_t orientation;
    bool priority;
    uint8_t requestedAction;
    uint8_t currentAction;
} RoadInfo;

/**
 * Action advertised by the car
 */
extern uint8_t requestedAction;

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
