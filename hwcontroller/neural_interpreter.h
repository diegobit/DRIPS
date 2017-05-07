#ifndef H_NEURAL_INTERPRETER
#define H_NEURAL_INTERPRETER

#include <Arduino.h>

typedef struct CrossroadStatus {
    bool left: 1;
    bool front: 1;
    bool right: 1;
} CrossroadStatus;

CrossroadStatus neuralInterpretate(uint16_t *fhtLeft, uint16_t *fhtFront, uint16_t *fhtRight);

#endif // H_NEURAL_INTERPRETER
