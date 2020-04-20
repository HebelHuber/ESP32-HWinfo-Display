
from machine import Pin, I2C
import sh1106

class StatDisplay(object):
  def __init__(self):
    
    # ESP32
    self.i2c = I2C(scl=Pin(22), sda=Pin(21), freq=400000)

    # ESP8266
    # self.i2c = I2C(scl=Pin(5), sda=Pin(4), freq=400000)

    self.display = sh1106.SH1106_I2C(128, 64, self.i2c, Pin(16), 0x3c)
    self.display.sleep(False)
    self.display.rotate(True)
    self.display.fill(0)

  def Lerp(self, v0, v1, t):
    return (1 - t) * v0 + t * v1
    
  def InvLerp(self, min, max, t):
    return (t - min) / (max - min);
    
  def GetTextWidth(self, val):
    text = str(val)
    return (len(text) * (8))
    
  def GetTextxOffset(self, val):
    return int(self.GetTextWidth(val) / 2)

  def ProgressBar(self, startx, starty, width, height, min, max, val):
    percentage = self.InvLerp(min,max,val)
    
    # the "frame"
    self.display.rect(startx,starty,width,height,1)
    
    # the "fill"
    percentageWidth = int(self.Lerp(0, width, percentage))
    self.display.fill_rect(startx,starty,percentageWidth,height,1)
    
    # calc text
    text = str(val)
    textwidth = (len(text) * (8))
    textxpos = int(startx + ((width - textwidth) / 2))
    textypos = int(starty + ((height - 8) / 2))
    
    # text BG
    textBGstartx = textxpos
    textBGstarty = textypos-1
    self.display.fill_rect(textBGstartx,textBGstarty,textwidth,9,0)
    
    if (startx+percentageWidth) > textBGstartx:
      self.display.pixel(textBGstartx,textBGstarty,1 )
      self.display.pixel(textBGstartx,textBGstarty+9-1,1 )
      
      if (startx+percentageWidth) > textBGstartx+textwidth:
        self.display.pixel(textBGstartx+textwidth-1,textBGstarty+9-1,1 )
        self.display.pixel(textBGstartx+textwidth-1,textBGstarty,1 )
    
    # text
    self.display.text(text, textxpos, textypos, 1)
    
    # make it look rounded
    self.display.pixel(startx,starty,0 )
    self.display.pixel(startx,starty+height-1,0 )
    self.display.pixel(startx+width-1,starty+height-1,0 )
    self.display.pixel(startx+width-1,starty,0 )
    
  def DrawProp(self, name, y, min, max, val):
    self.display.text(name, 0, y+1, 1)
    self.ProgressBar(44, y, 84, 10, min, max, val)
    return y+11
    
  def UpdateScreen(self, time, CPUl, CPUt, GPUl, GPUt, RAM):
    
    self.display.rotate(True)
    self.display.fill(0)
    
    self.display.text(time, self.GetTextxOffset(time), 0, 1)
    
    nextpos = self.DrawProp("CPU %", 10, 0,100,CPUl)
    nextpos = self.DrawProp("CPU T", nextpos, 0,100,CPUt)
    nextpos = self.DrawProp("GPU %", nextpos, 0,100,GPUl)
    nextpos = self.DrawProp("GPU T", nextpos, 0,100,GPUt)
    nextpos = self.DrawProp("RAM %", nextpos, 0,100,RAM)

    self.display.show()

if __name__ == "__main__":
  from machine import RTC
  drawer = StatDisplay()
  
  time = RTC().datetime()
  
  hour = str(time[4])
  if (len(hour)<2):
    hour = '0'+hour
    
  minute = str(time[5])
  if (len(minute)<2):
    minute = '0'+minute
    
  second = str(time[6])
  if (len(second)<2):
    second = '0'+second
  
  timetext = hour + ':' + minute + ':' + second
  drawer.UpdateScreen(timetext, 25, 80, 30, 90, 35)





