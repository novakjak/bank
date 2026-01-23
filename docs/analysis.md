# P2P Bankovní uzel – analýza

Autor: Martin Šilar  
Škola: SPŠE Ječná  
Datum: 16. 01. 2026  
Typ práce: Školní projekt  

## 1. Úvod

Tento dokument slouží jako analýza bankovního uzlu, který funguje v decentralizované P2P síti.
Cílem analýzy je popsat problém, kontext, uživatele a základní požadavky na systém, aniž by se řešilo konkrétní technické provedení.
Analýza vychází ze zadání od vyučujícího a shrnuje, co musí výsledná aplikace splňovat.

## 2. Kontext a popis problému

Systém představuje jeden bankovní uzel v P2P síti, kterou tvoří více nezávislých bank.
Každý uzel spravuje vlastní bankovní účty a zároveň komunikuje s ostatními uzly pomocí textového protokolu přes TCP/IP.

Aby mohla síť správně fungovat, musí mít všechny uzly jednotně definované chování a způsob komunikace.
Důležité je zejména to, aby bylo možné pracovat i s účty vedenými u jiné banky v síti a získat od ní správnou odpověď.

## 3. Cíloví uživatelé a aktéři

Systém pracuje s následujícími aktéry:

- **Uživatel**: Osoba nebo nástroj, který se k aplikaci připojuje přes TCP klienta (např. PuTTY nebo telnet) a zadává příkazy ručně.
- **Jiný bankovní uzel**: Jiná instance bankovní aplikace v P2P síti, se kterou probíhá vzájemná komunikace.

## 4. Funkční požadavky

- **FR-01** Systém musí umožnit identifikaci banky pomocí IP adresy.
- **FR-02** Systém musí umožnit vytvoření nového bankovního účtu s unikátním číslem.
- **FR-03** Systém musí umožnit vložení peněz na bankovní účet.
- **FR-04** Systém musí umožnit výběr peněz z bankovního účtu.
- **FR-05** Systém musí umožnit zjištění aktuálního zůstatku na účtu.
- **FR-06** Systém musí umožnit smazání bankovního účtu, pokud je jeho zůstatek nulový.
- **FR-07** Systém musí poskytovat informaci o celkové částce uložené v bance.
- **FR-08** Systém musí poskytovat informaci o počtu klientů banky.
- **FR-09** Pokud je příkaz určen pro jinou banku, systém musí zajistit jeho předání správnému bankovnímu uzlu (proxy režim).

## 5. Nefunkční požadavky

- **NFR-01** Komunikace mezi uzly musí probíhat pomocí TCP/IP.
- **NFR-02** Komunikační protokol musí být textový a používat kódování UTF-8.
- **NFR-03** Všechny zprávy musí být jednořádkové a ukončené znakem konce řádku.
- **NFR-04** Systém musí zvládnout obsluhu více klientů současně.
- **NFR-05** Každé síťové spojení musí mít nastavený timeout, standardně 5 sekund.
- **NFR-06** Stav banky musí být uložen trvale a nesmí se ztratit po restartu aplikace.
- **NFR-07** Aplikace musí zaznamenávat svůj provoz a komunikaci do logů.
- **NFR-08** Program musí být spustitelný na školním počítači bez použití IDE.
- **NFR-09** Aplikace musí naslouchat na portu v rozsahu 65525–65535.
- **NFR-10** Základní nastavení (port, IP adresa, timeouty) musí být konfigurovatelné.

## 6. Omezení a předpoklady

- Systém není určen pro reálný bankovní provoz ani práci s opravdovými financemi.
- Všechny částky jsou uvažovány v jedné měně bez řešení měnových kurzů.
- Zabezpečení komunikace není hlavním cílem projektu.
- Síť je tvořena omezeným počtem uzlů v rámci školního prostředí.
- Aplikace nemá grafické uživatelské rozhraní a je ovládána pouze pomocí textových příkazů.

## 7. Shrnutí

Tato analýza popisuje základní funkce, omezení a požadavky na bankovní P2P uzel.
Na jejím základě bude v další fázi navržen konkrétní způsob řešení a následně provedena implementace aplikace, která umožní spolupráci více bankovních uzlů v jedné síti.
