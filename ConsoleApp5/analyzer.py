from PIL import Image
import pytesseract
import speech_recognition as sr
import os
import re

def ImageRecognizer(file):
    try:
        img = Image.open(file)
        text = pytesseract.image_to_string(img, lang='rus')
        os.remove(file)
        return text
    except:
        print("error:"+ file)
    
mypath = "userphotos/"
files = os.listdir(mypath) 
result = str()
for file in files:
    result=result+ImageRecognizer(mypath+file)

result=re.sub(' +', ' ', result)
result=re.sub(' \r', ' ', result)
result=re.sub(' \n', ' ', result)

output_file = open("alalisys.txt", "w")
output_file.write(result)
output_file.close()