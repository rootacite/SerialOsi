

#include "ST7735.h"
#include "GFX_FUNCTIONS.h"

#include "mixplus.h"
#include "Analog.hpp"
#include "Timer.hpp"
#include "Exti.hpp"
#include "Serial.hpp"

#include "usbd_cdc_if.h"
#include "GPIO.hpp"


#include <string>

#define DATA_SIZE 1920

AnalogDMA Adc1(&hadc1,1);
Timer Timer9(&htim9);
GPIO PB12(GPIOB,GPIO_PIN_12);
Serial Serial1(&huart1);

bool Recived_Flag = false;

uint32_t Count = 0;
uint32_t Last = 0;

uint16_t *Front = nullptr;
uint16_t *Back = nullptr;

uint8_t Verification[4] = {255,255,255,255};

bool Front_Prepair = false;

uint32_t PointOfBack = 0;
bool Transmit_Enable = false;

bool Fist_Time = true;

void Swap()
{
    auto Temp = Front;
    Front=Back;
    Back=Temp;
}

void setup()
{
    Front = new uint16_t[DATA_SIZE];
    Back = new uint16_t[DATA_SIZE];

    Adc1.begin();
    Serial1.begin();
    PB12.set(0);

    Timer9.freq(96,500000);
    Timer9.circle(9);
    Timer9.ontick([](){
        if(PointOfBack==DATA_SIZE)
        {
            PointOfBack=0;
            Front_Prepair=true;
            Swap();
        }
        uint16_t vx;
        Adc1.get(&vx);

        Back[PointOfBack] = vx;
        PointOfBack++;
    });

    Exti::attachInterrupt(GPIO_PIN_0,[]()
    {
        PB12.toggle();
        Transmit_Enable=!Transmit_Enable;

        if(Transmit_Enable)
            Timer9.start();
        else
            Timer9.stop();
    });
}

void dataProc(uint8_t*data, uint32_t Len)
{
    char *bb=new char[Len+1];
    memcpy(bb,data,Len);
    bb[Len]='\0';

    Timer9.freq(96,atoi(bb) * 10);

    Serial1.write(bb,Len);
}

void loop()
{
    if(Front_Prepair)
    {
        if(Transmit_Enable && (!Fist_Time)) {
            CDC_Transmit_FS((uint8_t *) Front, DATA_SIZE * 2);
        }
        if(Fist_Time)Fist_Time=false;
        Front_Prepair=false;
    }
}