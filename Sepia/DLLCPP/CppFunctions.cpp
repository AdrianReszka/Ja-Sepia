#include "pch.h"
#include <iostream>
#include <windows.h>
#include <cmath> // Dodaj nag��wek dla std::floor i std::fmod


//Eksportowana funkcja ApplySepiaFilter, kt�ra przyjmuje zakres wierszy obrazu do przetworzenia
extern "C" __declspec(dllexport) void ApplySepiaFilterCpp(unsigned char* pixelBuffer, int width, int bytesPerPixel, int P, int startRow, int endRow, int stride)
{
    for (int y = startRow; y < endRow; y++)
    {
        //Pocz�tek bie��cego wiersza w buforze
        int rowStart = y * stride;

        for (int x = 0; x < width; x++)
        {
            //Indeks bie��cego piksela
            int pixelIndex = rowStart + x * bytesPerPixel;

            //Pobranie kana��w RGB z bufora
            unsigned char B = pixelBuffer[pixelIndex];     //Blue
            unsigned char G = pixelBuffer[pixelIndex + 1]; //Green
            unsigned char R = pixelBuffer[pixelIndex + 2]; //Red

            //Konwersja na odcie� szaro�ci
            int gray = static_cast<int>(std::round(0.299 * R + 0.587 * G + 0.114 * B));

            //Przekszta�cenie na efekt sepii
            int newB = gray;
            int newG = P + gray;
            int newR = 2 * P + gray;

            if (newB > 255) newB = 255;
            if (newB < 0) newB = 0;

            if (newG > 255) newG = 255;
            if (newG < 0) newG = 0;

            if (newR > 255) newR = 255;
            if (newR < 0) newR = 0;

            //Zapisanie przerobionych warto�ci RGB do bufora
            pixelBuffer[pixelIndex] = static_cast<unsigned char>(newB);     //Blue
            pixelBuffer[pixelIndex + 1] = static_cast<unsigned char>(newG); //Green
            pixelBuffer[pixelIndex + 2] = static_cast<unsigned char>(newR); //Red
        }
    }
}






