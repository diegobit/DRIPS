#ifndef H_CCS
#define H_CCS

#include <Arduino.h>

extern const uint16_t DELTA;
extern const uint16_t TIMESPAN_X;

extern bool advertiseCCS;

bool setupCCS(uint16_t *fhtLeft, uint16_t *fhtFront, uint16_t *fhtRight);
void handleCCS();
void readCCSMessages();
void sendKeepAlive();

#endif // H_CCS
