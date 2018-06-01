#include <SoftwareSerial.h>
#include <Adafruit_NeoPixel.h>

#define btRx 10
#define btTx 11
SoftwareSerial bluetooth(btRx, btTx);
#define btStatePin 7

#define btnPin 2

#define LEDPin 6
#define PIXEL_COUNT 8
Adafruit_NeoPixel LED = Adafruit_NeoPixel(PIXEL_COUNT, LEDPin, NEO_GRBW + NEO_KHZ800);//create the led interaction object from neoPixel library

//Used to manage the colors
typedef enum eState{
  BLUE,   //DISCONNECTED / NO ONE HERE
  GREEN,  //AVAILABLE
  ORANGE, //ON CALL
  RED     //OCCUPIED / AWAY
};
volatile eState state=BLUE;
eState oldState=BLUE;

//Used to periodically check connection and set timeout
typedef enum eCheck{
  NOTHING,
  CHECK,
  CHECKING,
  DISCONNECTED
};
volatile eCheck checkConnection=NOTHING;
bool isConnected=false;
//connection check frequency in seconds
#define connectionFreq 10
int lastCheck=0;

//Used to receive serial messages
String message="";

void setup() {
  bluetooth.begin(9600);//For bluetooth communication
  Serial.begin(9600);//For USB communication

  pinMode(btStatePin, INPUT);
  pinMode(btnPin, INPUT);
  attachInterrupt(digitalPinToInterrupt(btnPin), nextState, RISING);//When button down -> voltage rise

  LED.begin();
  setColor();//The default color is the BLUE for "nothing connected".

//setup timer interrupt
  cli();//stop interrupts
  
  //set timer1 interrupt at 1Hz
  TCCR1A = 0;// set entire TCCR1A register to 0
  TCCR1B = 0;// same for TCCR1B
  TCNT1  = 0;//initialize counter value to 0
  // set compare match register for 1hz increments
  OCR1A = 15624;// = (16*10^6) / (1*1024) - 1 (must be <65536)
  // turn on CTC mode (interrupt when reached the desired count)
  TCCR1B |= (1 << WGM12);
  // Set CS10 and CS12 bits for 1024 prescaler
  TCCR1B |= (1 << CS12) | (1 << CS10);  
  // enable timer compare interrupt
  TIMSK1 |= (1 << OCIE1A);
  
  sei();//allow interrupts
}

void loop() {
  //Color changement
  if(oldState!=state){
    setColor();
  }

  //Read incoming Serial message
  if(digitalRead(btStatePin)){
    if(bluetooth.available())
      message=bluetooth.readStringUntil('\n');
  }
  else{
    if(Serial.available())
      message=Serial.readStringUntil('\n');
  }
  //message parsing
  if(message!=""){
    //remove endline chars
    message.trim();
    if(message=="PING")
      sendSerial("PING");
    else if(message=="PONG"){
      isConnected=true;
      lastCheck=0;
      checkConnection=NOTHING;
    }
    else if (message=="GREEN")
      state=GREEN;
    else if (message=="ORANGE")
      state=ORANGE;
    else if (message=="RED")
      state=RED;
    message="";
  }

  //Check connection
  switch(checkConnection){
    case DISCONNECTED://Set as disconnected and check again
      isConnected=false;
      state=BLUE;
    case CHECK://Send a ping
      sendSerial("PING");
      checkConnection=CHECKING;
      lastCheck=0;
      break;
  }
}


//---------------------------------------------------
//Functions

void sendSerial(String msg){
  if(digitalRead(btStatePin))//If the bluetooth is connected, use it else send by USB
    bluetooth.println(msg);
  else
    Serial.println(msg);
}

//Set the color corresponding to the actual state, send new state via serial and update old state
void setColor(){
  //Set the correct color    
  uint32_t color;
  switch(state){
    case BLUE:
      color=LED.Color(0,0,255,0);
      //color=LED.Color(255,17,35,0);//THIS IS CHZ PINK!!
      sendSerial("BLUE");
      break;
    case GREEN:
      color=LED.Color(0,255,0,0);
      //color=LED.Color(0,200,255,0);//THIS IS KAREN TURQUOISE
      sendSerial("GREEN");
      break;
    case ORANGE:
      color=LED.Color(255,64,0,0);
      sendSerial("ORANGE");
      break;
    case RED:
      color=LED.Color(255,0,0,0);
      sendSerial("RED");
      break;
  }

  //Apply the color to each LED
  for(int i=0; i<PIXEL_COUNT; i++){
    LED.setPixelColor(i, color);
  }
  //Send the color to the led strip
  LED.show();

  oldState=state;
}


//---------------------------------------------------
//Interrupt Service Routines
//Button
void nextState(){
  if(oldState==state){
    switch(state){
      case BLUE:
        state=GREEN;
        break;
      case GREEN:
        state=ORANGE;
        break;
      case ORANGE:
        state=RED;
        break;
      case RED:
        state=BLUE;
        break;
    }
  }
}

//timer1 at 1Hz => every second
ISR(TIMER1_COMPA_vect){
  lastCheck++;
  if(lastCheck>=connectionFreq){
    switch(checkConnection){
      case NOTHING:
        checkConnection=CHECK;
        break;
      case CHECKING:
        if(isConnected)
          checkConnection=DISCONNECTED;
        else
          checkConnection=CHECK;
        break;
    }
  }
}

