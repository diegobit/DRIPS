#include <MsTimer2.h>
#include <arduinoFFT.h>

#define IR_LED 8
#define SENSOR A0

arduinoFFT FFT = arduinoFFT();

const uint16_t samples = 128; //This value MUST ALWAYS be a power of 2
double samplingFrequency = 200;

double vReal[samples];
double vImag[samples];

#define SCL_INDEX 0x00
#define SCL_TIME 0x01
#define SCL_FREQUENCY 0x02



void flash() {
  static boolean output = HIGH;

  digitalWrite(IR_LED, output);
  output = !output;
}

void PrintVector(double *vData, uint8_t bufferSize, uint8_t scaleType)
{
  for (uint16_t i = 0; i < bufferSize; i++)
  {
    double abscissa;
    /* Print abscissa value */
    switch (scaleType)
    {
      case SCL_INDEX:
        abscissa = (i * 1.0);
        break;
      case SCL_TIME:
        abscissa = ((i * 1.0) / samplingFrequency);
        break;
      case SCL_FREQUENCY:
        abscissa = ((i * 1.0 * samplingFrequency) / samples);
        break;
    }
    Serial.print(abscissa, 6);
    Serial.print(" ");
    Serial.print(vData[i], 4);
    Serial.println();
  }
  Serial.println();
}


void setup() {
  Serial.begin(9600);
  pinMode(SENSOR, INPUT);
  pinMode(IR_LED, OUTPUT);
  digitalWrite(IR_LED, HIGH);

  MsTimer2::set(25, flash); // 500ms period
  MsTimer2::start();
}

void loop() {
  Serial.println("\n *** Reading start ***\n");
  for (uint8_t i = 0; i < samples; i++) {
    vReal[i] = analogRead(SENSOR);
    vImag[i] = 0;
    delay(10);
  }
  Serial.println("\n *** Reading end ***\n");
  
  Serial.println("  ---  Data:");
  Serial.println(millis());
  //PrintVector(vReal, samples, SCL_TIME);
  FFT.Windowing(vReal, samples, FFT_WIN_TYP_HAMMING, FFT_FORWARD);  /* Weigh data */
  //Serial.println("Weighed data:");
  //PrintVector(vReal, samples, SCL_TIME);
  FFT.Compute(vReal, vImag, samples, FFT_FORWARD); /* Compute FFT */
  //Serial.println("Computed Real values:");
  //PrintVector(vReal, samples, SCL_INDEX);
  //Serial.println("Computed Imaginary values:");
  //PrintVector(vImag, samples, SCL_INDEX);
  FFT.ComplexToMagnitude(vReal, vImag, samples); /* Compute magnitudes */
  //Serial.println("Computed magnitudes:");
  //PrintVector(vReal, (samples >> 1), SCL_FREQUENCY);
  double x = FFT.MajorPeak(vReal, samples, samplingFrequency);
  //Serial.println(x, 6);
  Serial.println(millis());
  
  //Serial.println(val);
  //delay(100);
}
