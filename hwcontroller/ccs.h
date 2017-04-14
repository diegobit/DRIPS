#ifndef H_CCS
#define H_CCS

#include <Arduino.h>

extern uint8_t requestedAction; // Actual action advertised by the car
extern uint8_t visibleAction; // Action shown by the turn leds

void setupCCS();
void handleCCS();

#endif // H_CCS
