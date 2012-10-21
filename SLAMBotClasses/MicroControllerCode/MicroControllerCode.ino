#include <SoftwareSerial.h>

#define handshake 0
#define testLight 1
#define leftMotor 2
#define rightMotor 3

#define RX_PIN 0
#define pinTestLight 13
#define pinMotorController 3

#define MOTOR_BAUDRATE 38400

SoftwareSerial serialMotorController(RX_PIN, pinMotorController);

void setup()
{
  Serial.begin(9600);
  pinMode(pinTestLight, OUTPUT); 
  pinMode(pinMotorController, OUTPUT );
  digitalWrite(pinTestLight, LOW);
  
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
}
  

