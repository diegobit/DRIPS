#include "common.h"

#include <Arduino.h>

#if DEBUG
void __assert(bool success, String msg) {
    if (!success) {
        Serial.println(msg);
        Serial.flush();
        abort();
    }
}
#endif

RequestedAction requestedAction = ERA_STRAIGHT;
CurrentAction currentAction = ECA_STRAIGHT;
bool hasPriority = false;

RoadInfo crossroad[3];

void initCrossroad() {
    for (uint8_t i = 0; i < 3; i++) {
        memcpy(&(crossroad[i].manufacturer), &("        "), 8);
        memcpy(&(crossroad[i].model), &("        "), 8);
        crossroad[i].orientation = 0;
        crossroad[i].priority = false;
        crossroad[i].requestedAction = ERA_STRAIGHT;
        crossroad[i].currentAction = ECA_STRAIGHT;
    }
}