const int buttonPin = 4;
const int ledPin = 13;

int buttonState = 0;
int lastbuttonState = 0;

bool buttonPressed = false;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(ledPin, OUTPUT);
  pinMode(buttonPin, INPUT);
}

void loop() {
  // put your main code here, to run repeatedly:
  buttonState = digitalRead(buttonPin);
  if (buttonState == HIGH && lastbuttonState == LOW){
    if(!buttonPressed){
      buttonPressed = true;
      Serial.print("h\n");
    }
    
  }
  if(buttonState == LOW){
    buttonPressed = false;
  }
  lastbuttonState = buttonState;
}
