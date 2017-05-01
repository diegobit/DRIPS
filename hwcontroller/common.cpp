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

#define _8_SPACES { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' }

RoadInfo crossroad[3] = {
    { _8_SPACES, _8_SPACES, 0, false, ERA_STRAIGHT, ECA_STRAIGHT },
    { _8_SPACES, _8_SPACES, 0, false, ERA_STRAIGHT, ECA_STRAIGHT },
    { _8_SPACES, _8_SPACES, 0, false, ERA_STRAIGHT, ECA_STRAIGHT }
};