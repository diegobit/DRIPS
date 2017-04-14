#include "common.h"
#include "ccs.h"

#include <Arduino.h>
#include <SPI.h>       // nRF24L01+
#include <RH_NRF24.h>  // nRF24L01+

// Singleton instance of the radio driver
RH_NRF24 nrf24(10, 9); // CE, CS

extern uint8_t requestedAction; // Actual action advertised by the car
void test_radio();

void setupCCS() {
  if (!nrf24.init()) {
    Serial.println(F("Radio init failed!"));
  }
}

/**
 * This function is meant to be called periodically. It should complete
 * its work as quick as possible and maintain an internal state so that
 * it can continue the next time it get called.
 */
void handleCCS() {
  static uint8_t state = 0;
  switch (state) {
    case 0:
      test_radio();
      break;
  }
}

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
