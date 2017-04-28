#pragma GCC optimize ("O3")
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

/**
 * Time to wait between consecutive attempts to send a CCS.
 * Must be ≫ 2*TIMESPAN_X, and in general should be greater
 * than the maximum time required to complete a CCS procedure.
 */
const uint16_t TIMESPAN_NOOP = 0;

/** Time to wait from keepAliveTimeMarker before sending another keepAlive */
const unsigned long TIMESPAN_KEEPALIVE = 0;

/** Maximum backoff time for the KeepAlive */
const uint16_t TIMESPAN_KEEPALIVE_BACKOFF = 0;

/** Time after which vehicles in the vehicle cache expire */
const uint16_t VEHICLE_CACHE_TTL = 0;

/** Duration of the wait after sending a CCS, and duration of the blinking of the LEDs. */
const uint16_t TIMESPAN_X = 0;

/** Value for the max length of the random backoff interval (ms). */
const uint16_t TIMESPAN_Z = 0;

/** CCS led frequency */
const int CCS_K = 0;


// ==== TYPE DEFINITIONS ==== //

typedef enum State {
    ST_BEGIN,
    ST_WAIT_TO_BLINK,
    ST_BLINK,
    ST_INTERPRETATE,

    // The state returned by handlePeriodicActions() when no state change should occur.
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
State FUN_ST_BEGIN();
State FUN_ST_WAIT_TO_BLINK();
State FUN_ST_BLINK();
State FUN_ST_INTERPRETATE();
State handlePeriodicActions();
void sendKeepAlive();
void sendCCS();
bool isChannelFree();
inline bool isExpired(const Vehicle *vehicle);


// ==== VARIABLES ==== //

// Singleton instance of the radio driver
RH_NRF24 nrf24(10, 9); // CE, CS
extern uint8_t requestedAction; // Actual action advertised by the car
extern uint8_t currentAction; // The action agreed with the network
Vehicle vehicles[3]; // The vehicles cache with info received from the network
/**
 * This variable indicates the time at which the previous state ended.
 * In case of ST_BEGIN, it indicates the time at which the previous
 * protocol execution ended.
 *
 * Note that handlePeriodicActions() can updates the value too, in order to prepare
 * it for ST_BEGIN when resetting the procedure.
 */
unsigned long timeMarker = 0; // TODO REDUCE ACCURACY TO SAVE SPACE
unsigned long keepAliveTimeMarker = 0;
bool advertiseCCS = false;
uint16_t backoff = 0; // 0 means no backoff (*not* a zero-length backoff)
char currentPeer = '\0'; // TODO Remember to assign it where needed!
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
    switch (state) {
        case ST_BEGIN:
            state = FUN_ST_BEGIN();
        case ST_WAIT_TO_BLINK:
            state = FUN_ST_WAIT_TO_BLINK();
        case ST_BLINK:
            state = FUN_ST_BLINK();
        case ST_INTERPRETATE:
            state = FUN_ST_INTERPRETATE();
        default:
            Serial.println(F("INVALID STATE"));
    }
}

State FUN_ST_BEGIN() {
    State r = handlePeriodicActions();
    if (r != ST_CURRENT) {
        return r;
    }

    const uint16_t timeToWait = backoff == 0 ? TIMESPAN_NOOP : 2*TIMESPAN_X + backoff;
    if (millis() < timeMarker + timeToWait) {
        return ST_BEGIN;
    }

    backoff = 0;

    sendCCS();

    // TODO What if, instead of trying to send and then hoping nothing collides, we first
    // listen to the channel to see if someone's talking? Kind of what 802.11 does:
    // https://en.wikipedia.org/wiki/Received_signal_strength_indication
    //      Serial.print("RSSI: ");
    //      Serial.println((uint8_t)rf24.lastRssi(), DEC);

    timeMarker = millis();

    return ST_WAIT_TO_BLINK;
}

State FUN_ST_WAIT_TO_BLINK() {
    State r = handlePeriodicActions();
    if (r != ST_CURRENT) {
        return r;
    }

    if (millis() < timeMarker + TIMESPAN_X) { // TODO improve accuracy by waiting (TIMESPAN_X - half of the
                                              // time between consecutive received CCSs), only if I'm on
                                              // the receiver side
        return ST_WAIT_TO_BLINK;
    }

    advertiseCCS = true;
    timeMarker = millis();

    return ST_BLINK;
}

State FUN_ST_BLINK() {
    static bool sampled = false;
    const uint16_t expectedFhtTime = 1998 * _us; // TODO Big Comment
    const uint16_t expectedProcessingTime = 3 * ((uint16_t)SAMPLING_PERIOD * (uint16_t)FHT_N + expectedFhtTime) / 1000; // ms

    State r = handlePeriodicActions();
    if (r != ST_CURRENT) {
        return r;
    }

    if (millis() < timeMarker + TIMESPAN_X) {
        /**
         * Represents the time at which we should start sampling the data.
         *
         *   |------------- TIMESPAN_X -----------|
         *
         *   [........( sampling and fft )........]      <-- time diagram
         *
         *            |--------|
         *       expectedProcessingTime
         *
         * We compute the deadline time as follows:
         *     timeMarker                       // time at which ST_WAIT_TO_BLINK ended
         *   + (TIMESPAN_X / 2)                 // we center the start of the sampling within the total blinking period
         *   - (expectedProcessingTime / 2)     // we shift left so that the whole sampling is centered within the total blinking period
         */
        const unsigned long deadline = timeMarker + (TIMESPAN_X / 2) - (expectedProcessingTime / 2);

        if (!sampled && deadline < millis()) {
            // TODO Sample the fuck out of it
            sampled = true;
        }

        return ST_BLINK;
    }

    sampled = false;
    advertiseCCS = false;

    // TODO do sampling and FFT

    return ST_INTERPRETATE;
}

State FUN_ST_INTERPRETATE() {
    State r = handlePeriodicActions();
    if (r != ST_CURRENT) {
        return r;
    }

    timeMarker = millis();
    return ST_BEGIN;
}

/**
 * @return  if different than ST_CURRENT, indicates the state that the caller
 *          should immediately return.
 */
State handlePeriodicActions() {
    if (keepAliveTimeMarker + TIMESPAN_KEEPALIVE <= millis()) {
        sendKeepAlive();
        keepAliveTimeMarker = millis() + random(0, TIMESPAN_KEEPALIVE_BACKOFF);
    }

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

            } else if (buf[0] == MSG_TYPE_CCS) {
                // CCS
                const bool isForMe = buf[1] == ADDRESS[0];
                if (state == ST_BEGIN) {
                    if (isForMe) {
                        timeMarker = millis();
                        return ST_WAIT_TO_BLINK;
                    } else {
                        backoff = random(1, TIMESPAN_Z);
                        timeMarker = millis();
                        return ST_BEGIN;
                    }
                } else if (state == ST_WAIT_TO_BLINK || state == ST_BLINK) {
                    const char sender = buf[2];
                    if (!(isForMe && sender == currentPeer)) {
                        // Send pardoned SCS
                        uint8_t data[2]; // FIXME Must we send the string terminator too?
                        data[0] = MSG_TYPE_SCS;
                        data[1] = currentPeer;
                        nrf24.send(data, sizeof(data));
                        nrf24.waitPacketSent();
                    }
                } else if (state == ST_INTERPRETATE) {
                    if (isForMe) {
                        // Send non-pardoned SCS
                        uint8_t data[2]; // FIXME Must we send the string terminator too?
                        data[0] = MSG_TYPE_SCS;
                        data[1] = '0'; // FIXME Can we send \0 as the pardoned address?
                        nrf24.send(data, sizeof(data));
                        nrf24.waitPacketSent();
                    }
                }

            } else if (buf[0] == MSG_TYPE_SCS) {
                // SCS
                const bool pardoned = buf[1] == ADDRESS[0];
                if (state == ST_BEGIN || (state == ST_WAIT_TO_BLINK && !pardoned) || (state == ST_BLINK && !pardoned)) {
                    advertiseCCS = false;

                    // Choose backoff
                    backoff = random(1, TIMESPAN_Z);
                    timeMarker = millis();

                    // Instruct to go back to begin
                    return ST_BEGIN;
                }
            }
        }
    }

    return ST_CURRENT;
}

void sendKeepAlive() {
    uint8_t data[20];
    data[0] = MSG_TYPE_KEEPALIVE; // TODO e se invece tenessimo in ram direttamente l'array?
    data[1] = ADDRESS[0];
    data[2] = requestedAction;
    data[3] = currentAction;
    memcpy(&data[4], &(MANUFACTURER),  8);
    memcpy(&data[12], &(MODEL), 8);

    nrf24.send(data, sizeof(data));
    nrf24.waitPacketSent();
}

void sendCCS() {
    static uint8_t vehicleId = 0;

    // Find an unexpired vehicle to send the CCS using a Round-Robin policy
    for (uint8_t i = 1; i < 3+1; i++) {
        if (!isExpired(&vehicles[(i + vehicleId) % 3])) {
            vehicleId = (i + vehicleId) % 3;
            break;
        }
    }

    if (!isExpired(&vehicles[vehicleId])) {
        // The cache is not expired, which means an actual vehicle has been found
        uint8_t data[3];
        data[0] = MSG_TYPE_CCS;
        data[1] = ADDRESS[0];
        data[2] = vehicles[vehicleId].address;

        nrf24.send(data, sizeof(data));
        nrf24.waitPacketSent();
    }
}

bool isExpired(const Vehicle *vehicle) {
    return vehicle->receivedTime + VEHICLE_CACHE_TTL < millis();
}

/**
 * @return  true if no transmissions are heard on the channel
 */
bool isChannelFree() {
    // TODO implement
    return true;
}