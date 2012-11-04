#include <SoftwareSerial.h>
#include <AcceleroMMA7361.h>

AcceleroMMA7361 accelero;

#define handshake 0
#define testLight 1
#define leftMotor 2
#define rightMotor 3
#define xForce 4
#define yForce 5
#define zForce 6
#define temp 7
#define sendInfo 8

#define RX_PIN 0
#define pinTestLight 13
#define pinMotorController 3
#define sleepPin 12
#define selfTestPin 11
#define zeroGPin 10
#define gSelectPin 9
#define xPin A0
#define yPin A1
#define zPin A2
#define tempPin A3

#define MOTOR_BAUDRATE 38400

SoftwareSerial serialMotorController(RX_PIN, pinMotorController);

unsigned long lastInfoTransmit;
byte sendData;

void setup()
{
  Serial.begin(9600);
  pinMode(pinTestLight, OUTPUT); 
  pinMode(pinMotorController, OUTPUT );
  digitalWrite(pinTestLight, LOW);
  accelero.calibrate();
  
  accelero.begin(sleepPin, selfTestPin, zeroGPin, gSelectPin, xPin, yPin, zPin);
  accelero.setSensitivity(LOW);
  accelero.calibrate();
  lastInfoTransmit = millis();
  sendData = 0;
  
  serialMotorController.begin(MOTOR_BAUDRATE);
  delay(500);
  serialMotorController.write(64);
  delay(1);
  serialMotorController.write(192);
} 

void loop()
{
 if (Serial.available() >= 2)
 {
   parseInput(Serial.read(), Serial.read());   
 }
 
 if (sendData && millis() - lastInfoTransmit >= 200)
 {
   lastInfoTransmit = millis();
   byte x = map(accelero.getXAccel(), -600, 600, 0, 255);
   byte xBytes[] = { xForce, x };
   Serial.write(xBytes, 2);   
   
   byte y = map(accelero.getYAccel(), -600, 600, 0, 255);
   byte yBytes[] = { yForce, y };
   Serial.write(yBytes, 2);   
   
   byte z = map(accelero.getZAccel(), -600, 600, 0, 255);
   byte zBytes[] = { zForce, z };
   Serial.write(zBytes, 2);   
   
   int tempIn = analogRead(tempPin);
   double convertIn = map(tempIn, 0, 1023, 0, 5000);
   int tempOut = (convertIn - 500) / 10;
   tempOut += 50;
   byte tempBytes[] = { temp, tempOut };
   Serial.write(tempBytes, 2); 


   //Serial.write((byte)1);
   
   //   memcpy(xBytes,&x,4);
  // Serial.write(xBytes, 4);
   //Serial.write((byte)yForce);
   //Serial.write(accelero.getYAccel());
   //Serial.write((byte)zForce);
   //Serial.write(accelero.getZAccel());
   //Serial.write((byte)temp);
  // int tempIn = analogRead(tempPin);
   //double convertIn = map(tempIn, 0, 1023, 0, 5000);
   //double tempOut = (convertIn - 500) / 10;
   //Serial.write(tempOut);
 }
}

void parseInput(short cmd, short value)
{  
  if (cmd == handshake)
  {
    if (value == 0)
    {
      byte handshakeBuff[] = { handshake, 1 };
      Serial.write(handshakeBuff, 2);      
    }
   }
   else if (cmd == testLight)
   {
    if (value == 0)
      digitalWrite(pinTestLight, LOW);
    else
      digitalWrite(pinTestLight, HIGH);
   }
   else if (cmd == leftMotor)
   {     
     serialMotorController.write(value);    
   }
   else if (cmd == rightMotor)
   {
     serialMotorController.write(value); 
   }
   else if (cmd == sendInfo)
   {
     sendData = value;
   }
}

void integerToBytes(long val, byte b[4]) {
  b[3] = (byte )((val >> 24) & 0xff);
  b[2] = (byte )((val >> 16) & 0xff);
  b[1] = (byte )((val >> 8) & 0xff);
  b[0] = (byte )(val & 0xff); 
}
  

