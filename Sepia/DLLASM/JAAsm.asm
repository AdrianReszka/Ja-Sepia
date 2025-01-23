.data
;Wagi podane w formacie IEEE-754
weight_blue dd 1038710997  ; 0.114f - Waga B
weight_green dd 1058424226  ; 0.587f - Waga G
weight_red dd 1050220167  ; 0.299f - Waga R

;Mnozniki parametru P
p_multipliers db 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 0, 0, 0

;Wszystkie maski sa wyrownane do 16 bajtow - rejestry xmm to rejestry 16-bajtowe

;MASKI DO EKSPORTU KANALOW B G R

;Eksport kanalu B
blue_channel db 0, 80h, 80h, 80h, 3, 80h, 80h, 80h, 6, 80h, 80h, 80h, 9, 80h, 80h, 80h
;Maska po nalozeniu: B1 0 0 0 B2 0 0 0 B3 0 0 0 B4 0 0 0
;Kopiowane bity: bit0 0 0 0 bit3 0 0 0 bit6 0 0 0 bit9 0 0 0
;Po konwersji na 16bit: B1 0 B2 0 B3 0 B4 0
;Po konwersji na 32bit: B1 B2 B3 B4

;Eksport kanalu G
green_channel db 1, 80h, 80h, 80h, 4, 80h, 80h, 80h, 7, 80h, 80h, 80h, 10, 80h, 80h, 80h
;Maska po nalozeniu: G1 0 0 0 G2 0 0 0 G3 0 0 0 G4 0 0 0
;Kopiowane bity: bit1 0 0 0 bit4 0 0 0 bit7 0 0 0 bit10 0 0 0
;Po konwersji na 16bit: G1 0 G2 0 G3 0 G4 0
;Po konwersji na 32bit: G1 G2 G3 G4

;Eksport kanalu R
red_channel db 2, 80h, 80h, 80h, 5, 80h, 80h, 80h, 8, 80h, 80h, 80h, 11, 80h, 80h, 80h
;Maska po nalozeniu: R1 0 0 0 R2 0 0 0 R3 0 0 0 R4 0 0 0
;Kopiowane bity: bit2 0 0 0 bit5 0 0 0 bit8 0 0 0 bit11 0 0 0
;Po konwersji na 16bit: R1 0 R2 0 R3 0 R4 0
;Po konwersji na 32bit: R1 R2 R3 R4


;MASKI DO ZAPISU KANALOW B G R

;Niezaleznie od maski zapisujemy tylko bity 0, 4, 8 i 12
;Daje nam to mozliwosc powielenia od razu szarosci
;Ostatnie 4 bajty sa nieuzywane, wiec sa zerowane

;Zapis kanalu B
blue_save db 0, 80h, 80h, 4, 80h, 80h, 8, 80h, 80h, 12, 80h, 80h, 80h, 80h, 80h, 80h
;Maska po nalozeniu: gray1 0 0 gray2 0 0 gray3 0 0 gray4 0 0 0 0 0 0
;Kopiowane bity: bit0 0 0 bit4 0 0 bit8 0 0 bit12 0 0 0 0 0 0
green_save db 80h, 0, 80h, 80h, 4, 80h, 80h, 8, 80h, 80h, 12, 80h, 80h, 80h, 80h, 80h
;Maska po nalozeniu: 0 gray1 0 0 gray2 0 0 gray3 0 0 gray4 0 0 0 0 0
;Kopiowane bity: 0 bit0 0 0 bit4 0 0 bit8 0 0 bit12 0 0 0 0 0 
red_save db 80h, 80h, 0, 80h, 80h, 4, 80h, 80h, 8, 80h, 80h, 12, 80h, 80h, 80h, 80h
;Maska po nalozeniu: 0 0 gray1 0 0 gray2 0 0 gray3 0 0 gray4 0 0 0 0
;Kopiowane bity: 0 0 bit0 0 0 bit4 0 0 bit8 0 0 bit12 0 0 0 0

.code
ApplySepiaFilterAsm PROC
    ; Argumenty:
    ; rcx = pixelBuffer
    ; rdx = width
    ; r8 = bytesPerPixel
    ; r9 = stride
    ; P = [rsp + 28h]
    ; startRow = [rsp + 30h]
    ; endRow = [rsp + 38h]
    
    mov r10d, dword ptr [rsp + 30h] ; startRow -> r10d
    mov r11d, dword ptr [rsp + 38h] ; endRow -> r11d

    mov rbx, rcx ; rcx -> rbx

    movdqu xmm5, xmmword ptr [p_multipliers] ; p_multipliers -> xmm5
    movd xmm6, dword ptr [rsp + 28h] ; P -> xmm6
    pshufd xmm6, xmm6, 0 ; powielenie P na calym rejestrze xmm6
    pmulld xmm6, xmm5 ; przemnozenie przez p_multipliers
    
    vbroadcastss xmm7, weight_blue ; weight_blue -> xmm7 (+ rozprowadzenie)
    vbroadcastss xmm8, weight_green ; weight_green -> xmm8 (+ rozprowadzenie)
    vbroadcastss xmm9, weight_red ; weight_red -> xmm9 (+ rozprowadzenie)

    movdqu xmm10, xmmword ptr [blue_channel] ; blue_channel -> xmm10
    movdqu xmm11, xmmword ptr [green_channel] ; green_channel -> xmm11
    movdqu xmm12, xmmword ptr [red_channel] ; red_channel -> xmm12

    movdqu xmm13, xmmword ptr [blue_save] ; blue_save -> xmm13
    movdqu xmm14, xmmword ptr [green_save] ; green_save -> xmm14
    movdqu xmm15, xmmword ptr [red_save] ; red_save -> xmm15

RowLoop:
    cmp r10d, r11d ; Jesli obecny wiersz odpowiada koncowemu
    jge EndProc    ; skocz do zakonczenia procesu

    mov rcx, rbx ; rbx -> rcx
    mov eax, r10d ; obecny wiersz -> eax
    imul eax, r9d ; eax * stride
    add rcx, rax ; ecx -> eax

    xor r13, r13 ; zerowanie licznika pikseli

PixelLoop:
    cmp r13d, edx ; Jesli licznik pikseli odpowiada szerokosci obrazu
    jge NextRow   ; skocz do przejscia do nastepnego wiersza

    movlps xmm0, qword ptr [rcx] ; ladowanie pierwszych 64 bitow (8 bajtow) spod wskaznika rcx do gornej czesci xmm0
    movd xmm1, dword ptr [rcx+8] ; ladowanie kolejnych 32 bitow (4 bajtow) spod wskaznika rcx do dolnej czesci xmm1
    movlhps xmm0, xmm1 ; laczenie obu rejestrow - kopiowanie 32 bitow z dolnej czesci xmm1 na wolne miejsca w gornej czesci xmm0 - teraz mamy 96 bitow w xmm0 czyli to czego szukalismy

    movdqa xmm2, xmm0 ; kopiowanie zawartosci xmm0 do xmm2
    pshufb xmm2, xmm10 ; wyciagniecie kanalu B
    mulps xmm2, xmm7 ; mnozenie przez wage B - mnoze przez wartosci 32bitowe i teraz mam finalna postac rejestru

    movdqa xmm3, xmm0 ; kopiowanie zawartosci xmm0 do xmm3
    pshufb xmm3, xmm11 ; wyciagniecie kanalu G
    mulps xmm3, xmm8 ; mnozenie przez wage G

    movdqa xmm4, xmm0 ; kopiowanie zawartosci xmm0 do xmm4
    pshufb xmm4, xmm12 ; wyciagniecie kanalu R
    mulps xmm4, xmm9 ; mnozenie przez wage R

    paddd xmm2, xmm3 ; B + G [dod. 32-bitowe]
    paddd xmm2, xmm4 ; (B + G) + R [dod. 32-bitowe]
    ;xmm2 = gray1 0 0 0 gray2 0 0 0 gray3 0 0 0 gray4 0 0 0

    movdqa xmm0, xmm2 ; kopiowanie sumy kanalow do xmm0
    pshufb xmm0, xmm13 ; wyodrebnienie kanalu B do zapisu

    movdqa xmm1, xmm2 ; kopiowanie sumy kanalow do xmm1
    pshufb xmm1, xmm14 ; wyodrebnianie kanalu G do zapisu

    movdqa xmm3, xmm2 ; kopiowanie sumy kanalow R do xmm3
    pshufb xmm3, xmm15 ; wyodrebnianie kanalu R do zapisu

    paddb xmm3, xmm1 ; laczenie xmm1 z xmm3 (kanaly R i G) [dod. 8-bitowe]
    paddb xmm3, xmm0 ; laczenie xmm0 z xmm3 (kanaly R, G i B) [dod. 8-bitowe]
    ;xmm3 = gray1 gray1 gray1 gray2 gray2 gray2 gray3 gray3 gray3 gray4 gray4 gray4 0 0 0 0

    paddusb xmm3, xmm6 ; dodawanie parametru P (0, P, 2P) z ograniczeniem (0-255)

    movq qword ptr [rcx], xmm3 ; zapisanie pierwszych 64 bitow
    movhlps xmm3, xmm3 ; przeniesienie dolnych zajetych bitow xmm3 na poczatek rejestru
    movd dword ptr [rcx + 8], xmm3 ; zapisanie pozostalych 32 bitow

    mov r12, 4 ; r12 = 4
    imul r12, r8 ; r12 * bytesPerPixel
    add rcx, r12 ; rcx += r12
    add r13d, 4 ; r13 += 4
    jmp PixelLoop ; Skok na poczatek petli

NextRow:
    inc r10d ; r10++
    jmp RowLoop ; Skok do petli wierszy

EndProc:
    ret
ApplySepiaFilterAsm ENDP
END