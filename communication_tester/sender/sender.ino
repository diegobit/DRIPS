#include <SPI.h>
#include <RH_NRF24.h>

// Singleton instance of the radio driver
RH_NRF24 rf24(10, 9); // CE, CS

void setup() {
  Serial.begin(9600);
  if (!rf24.init())
    Serial.println("init failed");
  // The default radio config is for 30MHz Xtal, 434MHz base freq 2GFSK 5kbps 10kHz deviation
  // power setting 0x10
  // If you want a different frequency mand or modulation scheme, you must generate a new
  // radio config file as per the RH_RF24 module documentation and recompile
  // You can change a few other things programatically:
  //rf24.setFrequency(435.0); // Only within the same frequency band
  //rf24.setTxPower(0x7f);
}

void loop() {
  Serial.println("Sending to rf24_server");
  // Send a message to rf24_server
//  uint8_t data[100];
//  unsigned long now = millis();
//  String(now).toCharArray(data, 100);
//  Serial.write(data, 100);
//  Serial.write('\n');
  uint8_t data[] = "Hello Da!";
  
  rf24.send(data, sizeof(data));
  
  rf24.waitPacketSent();
  // Now wait for a reply
  uint8_t buf[RH_NRF24_MAX_MESSAGE_LEN];
  uint8_t len = sizeof(buf);
  if (rf24.waitAvailableTimeout(2000))
  { 
    // Should be a reply message for us now   
    if (rf24.recv(buf, &len))
    {
      Serial.print("Got reply: ");
      Serial.println((char*)buf);
    }
    else
    {
      Serial.println("recv failed");
    }
  }
  else
  {
    Serial.println("[No reply, is rf24_server running?]");
  }
  delay(400);
}
