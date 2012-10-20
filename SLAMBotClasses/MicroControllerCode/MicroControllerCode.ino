#define handshake 0
#define testLight 1

int pinTestLight = 13;

void setup()
{
  Serial.begin(9600);
  pinMode(pinTestLight, OUTPUT); 
  digitalWrite(pinTestLight, LOW);
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
}
  
