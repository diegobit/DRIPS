#ifndef H_COMMON
#define H_COMMON

#include <Arduino.h>

#define DEBUG 1

enum Actions {
  EA_NONE = 0,
  EA_TURN_LEFT = 1,
  EA_TURN_RIGHT = 2,
  EA_PRIORITY = 3
};

/**
 * Action advertised by the car
 */
extern uint8_t requestedAction;

#if DEBUG
void __assert(bool success, String msg);
#endif

#endif // H_COMMON
