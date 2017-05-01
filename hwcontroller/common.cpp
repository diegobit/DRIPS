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
