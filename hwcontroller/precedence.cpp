#include "precedence.h"
#include "common.h"
#include <Arduino.h>

/**
 * Takes an index of `crossroads` and returns the number of the lane the car is in
 * `3` is a valid argument, it's not an index of the array, but means 'this car'
 *
 * position = 0 (left car),  returns 7;
 * position = 1 (front car), returns 5;
 * position = 2 (right car), returns 3;
 * position = 3 (this car), returns 1;
 */
uint8_t positionToLaneNumber(uint8_t position) {
    return 7 - (position * 2);
}

/**
 * Takes a lane number and returns an index of `crossroads`, the car that is in that lane
 *
 * lane = 7, returns 0 (left car);
 * lane = 5, returns 1 (front car);
 * ...
 */
uint8_t laneNumberToPosition(uint8_t lane) {
    return (7 - lane) / 2;
}

/**
 * Takes an index of `crossroads` (a car) and returns the number of the lane the car wants to get into
 * `3` is a valid argument, it's not an index of the array, but means 'this car'
 *
 * If the action of a car that might cross our right is unknown, we assume the worst and return 9 (a
 * number bigger than any possible target lane, so who calls this method will always assume to be
 * crossed until the real action is known and the real target lane is computed)
 */
uint8_t carActionToLaneNumber(uint8_t carIndex) {
    const uint8_t carPos = positionToLaneNumber(carIndex);

    // Need to do this because 3 is not an index of `crossroads`
    const RequestedAction action = carIndex == 3 ? requestedAction : crossroad[carIndex].requestedAction;

    // Now 'add' 1 to get the lane to the right, 3 to the one ahead, 5 to the one to the left
    switch (action) {
        case ERA_TURN_RIGHT:
            return carPos + 1;
        case ERA_STRAIGHT:
            return carPos + 3;
        case ERA_TURN_LEFT:
            return carPos + 5;
        default:
            return 9; // 9 is bigger than any possible target lane
    }
}

/**
 * Returns true if there's a car with a requestedAction that would make it cross the right side of my requestedAction
 * 
 * We number the lanes of the crossroad from 1 to 8, counterclockwise, starting from this car's positions:
 *
 *          |     |     |
 *          |     |     |
 *          |  5  |  4  |
 *   -------             -------
 *         6             3
 *   -------             -------
 *         7             2
 *   -------             -------
 *          |  8  |  1  |
 *          |     |     |
 *          |     |     |
 *
 * The cars that may cross my right are the ones in lane [2, myTargetLane).
 *
 * A car crosses my right if carTargetLane >= myTargetLane
 *
 * Example: A wants to go from 1 to 6. B or C may cross my right (not D, in fact, D can only cross
 * A's left, thus D would have to give A precedence). In order for this to happen, they
 * would have to go to lane 6 or 8.
 *
 *          |     |     |
 *          |     |     |
 *          |  B  |     |
 *   -------             -------
 *        <-- *          C
 *   -------     *       -------
 *         D       *     
 *   -------        *    -------
 *          |     |  A  |
 *          |     |     |
 *          |     |     |
 */
bool someoneCrossesMyRight() {
    const uint8_t targetLane = carActionToLaneNumber(3);
    for (uint8_t l = 3; l < targetLane; l += 2) {
        // For every odd lane after me and before my target lane, check whether his action crosses my right
        const uint8_t carIndex = laneNumberToPosition(l);
        if (crossroad[carIndex].validUntil > millis()) {
            const uint8_t otherTargetLane = carActionToLaneNumber(carIndex);
            if (otherTargetLane >= targetLane) {
                return true;
            }
        }
    }
    return false;
}

bool someoneHasPriority() {
    return crossroad[0].priority || crossroad[1].priority || crossroad[2].priority;
}

void computeCurrentAction() {
    if (hasPriority) {
        currentAction = static_cast<CurrentAction>(requestedAction); // FIXME not safe, but we know the underlying type and values match when assigning a requestedAction to a currentAction
    } else if (someoneCrossesMyRight() || someoneHasPriority()) {
        currentAction = ECA_STILL;
    } else {
        currentAction = static_cast<CurrentAction>(requestedAction); // FIXME not safe, but we know the underlying type and values match when assigning a requestedAction to a currentAction
    }
}