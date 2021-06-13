from PIL import Image
import pytesseract
import os
import re

def ImageRecognizer(file):
    try:
        img = Image.open(file)
        text = "Русский\n"+pytesseract.image_to_string(img, lang='rus') + "\nАнглийский\n" +pytesseract.image_to_string(img, lang='eng')
        os.remove(file)
        return text
    except:
        print("error:"+ file)

def AnalyzePhotos():
    mypath = "userphotos/"
    files = os.listdir(mypath) 
    result = str()
    for file in files:
        result=result+ImageRecognizer(mypath+file)

    result=re.sub(' +', ' ', result)
    result=re.sub(' \r', ' ', result)
    result=re.sub(' \n', ' ', result)

    output_file = open("analisys_photos.txt", "w", encoding='utf-8')
    output_file.write(result)
    output_file.close()

AnalyzePhotos()