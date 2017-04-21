#include "common.h"
#include "ccs.h"

#include <Arduino.h>
#include <SPI.h>       // nRF24L01+
#include <RH_NRF24.h>  // nRF24L01+
#include <limits.h>

// ==== CONSTANTS ==== //

#define MSG_TYPE_KEEPALIVE 'K'
#define MSG_TYPE_CCS 'C'
#define MSG_TYPE_SCS 'S'

/** Time to wait between consecutive attempts to send a CCS */
const uint16_t TIMESPAN_NOOP = 0;

/** Interval at which we send KeepAlive messages */
const unsigned long TIME_KEEPALIVE = 0;

/** Time after which vehicles in the vehicle cache expire */
const uint16_t VEHICLE_CACHE_TTL = 0;

const uint16_t TIMESPAN_X = 0;
const uint16_t TIMESPAN_Y = 0;
const uint16_t TIMESPAN_Z = 0;

/** CCS led frequency */
const int CCS_K = 0;


// ==== TYPE DEFINITIONS ==== //

typedef enum State {
  ST_BEGIN,
  ST_WAIT_TO_BLINK,
  ST_BLINK,
  ST_SAMPLE_L,
  ST_SAMPLE_F,
  ST_SAMPLE_R,
  ST_INTERPRETATE,

  // The state returned by handleIncomingRequests() when no state change should occur.
  // Never return this from a state handler.
  ST_CURRENT
} State;

typedef struct Vehicle {
  char address;
  char manufacturer[8];
  char model[8];
  bool priority;
  uint8_t requestedAction;
  uint8_t currentAction;
  unsigned long receivedTime;
} Vehicle;


// ==== FUNCTION DECLARATIONS ==== //
void test_radio();
State FUN_ST_BEGIN();
State FUN_ST_WAIT_TO_BLINK();
State FUN_ST_BLINK();
State FUN_ST_INTERPRETATE();
State handleIncomingRequests();
State stateJmp(State s);


// ==== VARIABLES ==== //

// Singleton instance of the radio driver
RH_NRF24 nrf24(10, 9); // CE, CS
extern uint8_t requestedAction; // Actual action advertised by the car
Vehicle vehicles[3];
/**
 * This variable indicates the time at which the previous state ended.
 * In case of ST_BEGIN, it indicates the time at which the previous
 * protocol execution ended.
 *
 * Note that handleIncomingRequests() can updates the value too, in order to prepare
 * it for ST_BEGIN when resetting the procedure.
 */
unsigned long timeMarker = 0; // TODO REDUCE ACCURACY TO SAVE SPACE
bool advertiseCCS = false;
uint16_t backoff = 0;
State state = ST_BEGIN;


// ==== FUNCTION IMPLEMENTATIONS ==== //

void setupCCS() {
  if (!nrf24.init()) {
    Serial.println(F("Radio init failed!"));
  }

  timeMarker = millis();
}

/**
 * This function is meant to be called periodically. It should complete
 * its work as quick as possible and maintain an internal state so that
 * it can continue the next time it get called.
 */
void handleCCS() {
  state = stateJmp(state);
}

State FUN_ST_BEGIN() {
  State r = handleIncomingRequests();
  if (r != ST_CURRENT) {
    return r;
  }

  if (millis() < timeMarker + TIMESPAN_NOOP + backoff) {
    return ST_BEGIN;
  }

  backoff = 0;

  // FIXME This is probably nonsense
  uint8_t data[] = "C0" ADDRESS;
  nrf24.send(data, sizeof(data));
  nrf24.waitPacketSent();

  timeMarker = millis();

  return ST_WAIT_TO_BLINK;
}

State FUN_ST_WAIT_TO_BLINK() {
  if (millis() < timeMarker + TIMESPAN_X) {
    return ST_WAIT_TO_BLINK;
  }

  advertiseCCS = true;
  timeMarker = millis();

  return ST_BLINK;
}

State FUN_ST_BLINK() {
  if (millis() < timeMarker + TIMESPAN_X) {
    return ST_BLINK;
  }

  advertiseCCS = false;

  return ST_SAMPLE_L;
}

State FUN_ST_INTERPRETATE() {
  timeMarker = millis();
  return ST_BEGIN;
}

/**
 * @return  whether the caller should reset the state to ST_BEGIN
 */
State handleIncomingRequests() {
  if (nrf24.available())
  {
    // Should be a message for us now
    uint8_t buf[RH_NRF24_MAX_MESSAGE_LEN];
    uint8_t len = sizeof(buf);
    if (nrf24.recv(buf, &len))
    {
      if (buf[0] == MSG_TYPE_KEEPALIVE) {
        // KeepAlive

        // find vehicle in the cache
        uint8_t index = 255; // 255 = not found
        for (uint8_t i = 0; i < 3; i++) {
          if (vehicles[i].address == buf[1]) {
            index = i;
            break;
          }
        }

        if (index == 255) {
          // vehicle wasn't in the cache,
          // pick oldest entry in vehicles
          unsigned long min = ULONG_MAX;
          for (uint8_t i = 0; i < 3; i++) {
            if (vehicles[i].receivedTime < min) {
              min = vehicles[i].receivedTime;
              index = i;
            }
          }
        }

        // replace the oldest entry with the new info
        vehicles[index].address = buf[1];
        vehicles[index].requestedAction = buf[2];
        vehicles[index].currentAction = buf[3];
        memcpy(&(vehicles[index].manufacturer), &buf[4], 8);
        memcpy(&(vehicles[index].model), &buf[12], 8);
        vehicles[index].priority = buf[13];
        vehicles[index].receivedTime = millis();
      }
    } else if (buf[0] == MSG_TYPE_CCS) {
      // CCS

    } else if (buf[0] == MSG_TYPE_SCS) {
      // SCS
      if (state == ST_BEGIN || state == ST_WAIT_TO_BLINK || state == ST_BLINK) {
        // Check if I'm pardoned
        if (buf[1] != ADDRESS[0]) {
          advertiseCCS = false;

          // Choose backoff
          backoff = random(TIMESPAN_Y, TIMESPAN_Z);
          timeMarker = millis();

          // Instruct to go back to begin
          return ST_BEGIN;
        }
      }
    }
  }

  return ST_CURRENT;
}

/**
 * Immediately invokes state s, then returns the new state requested by s.
 */
State stateJmp(State s) {
  switch (s) {
    case ST_BEGIN:
      return FUN_ST_BEGIN();
    case ST_WAIT_TO_BLINK:
      return FUN_ST_WAIT_TO_BLINK();
    case ST_BLINK:
      return FUN_ST_BLINK();
    case ST_INTERPRETATE:
      return FUN_ST_INTERPRETATE();
    default:
      Serial.println(F("UNHANDLED STATE JMP"));
      return ST_CURRENT;
  }
}

// FIXME Delete me
void test_radio() {
  if (nrf24.available())
  {
    // Should be a message for us now
    uint8_t buf[RH_NRF24_MAX_MESSAGE_LEN];
    uint8_t len = sizeof(buf);
    if (nrf24.recv(buf, &len))
    {
      // RF24::printBuffer("request: ", buf, len);
      Serial.print(F("got request: "));
      Serial.println((char*)buf);
      // Serial.print("RSSI: ");
      // Serial.println((uint8_t)rf24.lastRssi(), DEC);

      // Send a reply
      uint8_t data[] = "Hello Back";
      nrf24.send(data, sizeof(data));
      nrf24.waitPacketSent();
      Serial.println(F("Sent a reply"));
    } else {
      Serial.println(F("recv failed"));
    }
  }
}